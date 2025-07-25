using Extension.Commands;
using Extension.Helper;
using Extension.UI.ViewModels;
using Extension.UI.Windows;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Extension
{
    [Command(PackageIds.AddStringToResourceCommandId)]
    internal sealed class AddStringToResourceCommand : BaseCommand<AddStringToResourceCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var documentView = await VS.Documents.GetActiveDocumentViewAsync();
            if (documentView?.TextView == null)
            {
                //not a text window
                return;
            }

            var selection = documentView.TextView.Selection;
            if (selection.IsEmpty)
            {
                return;
            }

            var selectedText = documentView.TextBuffer.CurrentSnapshot.GetText(
                selection.Start.Position,
                selection.End.Position - selection.Start.Position
                );

            var resxGroups = await BuildResxGroupsAsync();
            if (resxGroups.Count == 0)
            {
                await VS.MessageBox.ShowErrorAsync(
                    $"No resx found in current project"
                    );
                return;
            }

            var neutralComment = await ComposeNeutralCommentAsync(
                documentView
                );

            var vm = new AddStringViewModel(
                neutralComment,
                resxGroups,
                selectedText
                );
            var w = new AddStringWindow(
                );
            w.DataContext = vm;
            if (!(await w.ShowDialogAsync()).GetValueOrDefault())
            {
                return;
            }
            var result = vm.Result;
            if (result is null)
            {
                return;
            }

            var targetNamespace = await result.Group.TryDetermineTargetNamespaceAsync(
                );

            var documentExtension = new FileInfo(documentView.FilePath).Extension;
            switch (documentExtension)
            {
                case ".xaml":
                    if (!XamlProcessor.ProcessDocument(
                        documentView,
                        result,
                        targetNamespace
                        ))
                    {
                        await VS.MessageBox.ShowErrorAsync(
                            "Error happened while xaml file editing"
                            );
                        return;
                    }
                    break;
                case ".cs":
                    if (!await CSharpProcessor.ProcessDocumentAsync(
                        documentView,
                        result,
                        targetNamespace
                        ))
                    {
                        await VS.MessageBox.ShowErrorAsync(
                            "Error happened while cs file editing"
                            );
                        return;
                    }
                    break;
                default:
                    await VS.MessageBox.ShowErrorAsync(
                        $"{documentExtension} files are not supported yet. Fill the issue, please."
                        );
                    return;
            }

            await result.Group.RebuildDesignerForResxFileAsync();
        }

        private static async System.Threading.Tasks.Task<List<ResxGroup>> BuildResxGroupsAsync(
            )
        {
            var project = await VS.Solutions.GetActiveProjectAsync();
            var resxs = await project.ProcessDownRecursivelyForAsync(
                p => !p.IsNonVisibleItem
                    && p.Type == SolutionItemType.PhysicalFile
                    && !string.IsNullOrEmpty(p.FullPath)
                    && new FileInfo(p.FullPath).Extension == ".resx"
                    ,
                CancellationToken.None
                );

            var resxGroups = (
                from resx in resxs
                let filePath = new FileInfo(resx.FullPath)
                let folderPath = filePath.Directory.FullName
                group resx by folderPath into rgroup
                select new ResxGroup(rgroup.Key, project, rgroup)
                ).ToList();

            return resxGroups;
        }

        private static async System.Threading.Tasks.Task<string> ComposeNeutralCommentAsync(
            DocumentView documentView
            )
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            var relative = documentView.FilePath.MakeRelativeAgainst(solution.FullPath);
            var neutralComment = $"Used in {relative} document";
            return neutralComment;
        }
    }
}
