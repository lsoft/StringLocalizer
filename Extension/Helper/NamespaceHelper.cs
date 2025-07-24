using Community.VisualStudio.Toolkit;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.Helper
{
    public static class NamespaceHelper
    {
        public static async Task<string?> TryDetermineTargetNamespaceAsync(
            this SolutionItem project,
            string documentFilePath
            )
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (documentFilePath is null)
            {
                throw new ArgumentNullException(nameof(documentFilePath));
            }

            var projectFolderPath = new FileInfo(project.FullPath).Directory.FullName;
            var documentFolderPath = new FileInfo(documentFilePath).Directory.FullName;

            if (documentFolderPath.Length < projectFolderPath.Length || !documentFolderPath.StartsWith(projectFolderPath))
            {
                return null;
            }

            var names = new List<string>();
            var dir = new DirectoryInfo(documentFolderPath);
            while (dir.FullName != projectFolderPath && dir.FullName.Length > projectFolderPath.Length)
            {
                names.Add(dir.Name);
                dir = dir.Parent;
            }

            names.Reverse();
            names.Insert(0, await GetProjectDefaultNamespaceAsync(documentFilePath, project));

            var targetNamespace = string.Join(".", names);

            return targetNamespace;
        }

        private static async Task<string> GetProjectDefaultNamespaceAsync(
            string documentFilePath,
            SolutionItem project
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

            var pi = dte.Solution.FindProjectItem(documentFilePath);
            if (pi != null)
            {
                var cp = pi.ContainingProject;
                if (cp != null)
                {
                    if (cp.Kind.In(SolutionHelper.CSharpProjectKind, SolutionHelper.DatabaseProjectKind))
                    {
                        var prop = cp.Properties;
                        if (prop != null)
                        {
                            var dn = prop.Item("DefaultNamespace");
                            if (dn != null)
                            {
                                return dn.Value.ToString();
                            }
                        }
                    }
                }
            }

            var dotIndex = project.Name.LastIndexOf(".");
            if (dotIndex <= 0)
            {
                return project.Name;
            }

            var result = project.Name.Substring(0, dotIndex);
            return result;
        }

    }
}
