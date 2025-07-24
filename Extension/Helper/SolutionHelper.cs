using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Extension.Helper
{
    public static class SolutionHelper
    {
        public const string DatabaseProjectKind = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
        public const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

        public static bool TryGetSolution(out Solution solution)
        {
            solution = VS.Solutions.GetCurrentSolution();
            if (solution is null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(solution.Name))
            {
                solution = null;
                return false;
            }

            return true;
        }


        public static async System.Threading.Tasks.Task<List<SolutionItem>> ProcessDownRecursivelyForSelectedAsync(
            Predicate<SolutionItem> predicate,
            CancellationToken cancellationToken
            )
        {
            var result = new List<SolutionItem>();

            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = (await sew.GetSelectionAsync()).ToList();
            if (selections.Count == 0)
            {
                return result;
            }

            foreach (var selection in selections)
            {
                var selectionChildren = await selection.ProcessDownRecursivelyForAsync(
                    predicate,
                    cancellationToken
                    );
                result.AddRange(selectionChildren);
            }

            return result;
        }

        public static T ConvertRecursivelyFor<T>(
            this SolutionItem item,
            Func<SolutionItem, T?> converter,
            Action<T, T> childAdder,
            CancellationToken cancellationToken
            )
            where T : class
        {
            //https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/issues/401
            item.GetItemInfo(out IVsHierarchy hierarchy, out uint itemID, out _);
            if (HierarchyUtilities.TryGetHierarchyProperty(hierarchy, itemID, (int)__VSHPROPID.VSHPROPID_IsNonMemberItem, out bool isNonMemberItem))
            {
                if (isNonMemberItem)
                {
                    // The item is not usually visible. Skip it.
                    return null;
                }
            }

            var root = converter(item);
            if (root is null)
            {
                return null;
            }

            foreach (var child in item.Children)
            {
                if (child == null)
                {
                    continue;
                }

                var cChild = ConvertRecursivelyFor(
                    child,
                    converter,
                    childAdder,
                    cancellationToken
                    );
                if (cChild is null)
                {
                    continue;
                }

                childAdder(root, cChild);
            }

            return root;
        }



        public static async Task<List<SolutionItem>> ProcessDownRecursivelyForAsync(
            this SolutionItem item,
            SolutionItemType[] types,
            string? fullPath,
            CancellationToken cancellationToken
            )
        {
            var foundItems = new FoundSolutionItems();
            await ProcessDownRecursivelyForAsync(
                foundItems,
                item,
                item => item.Type.In(types) && (string.IsNullOrEmpty(fullPath) || fullPath == item.FullPath),
                cancellationToken
                );
            return foundItems.Result;
        }

        public static async Task<List<SolutionItem>> ProcessDownRecursivelyForAsync(
            this SolutionItem item,
            Predicate<SolutionItem> predicate,
            CancellationToken cancellationToken
            )
        {
            var foundItems = new FoundSolutionItems();
            await ProcessDownRecursivelyForAsync(foundItems, item, predicate, cancellationToken);
            return foundItems.Result;
        }

        private static async Task ProcessDownRecursivelyForAsync(
            FoundSolutionItems foundItems,
            SolutionItem item,
            Predicate<SolutionItem> predicate,
            CancellationToken cancellationToken
            )
        {
            //https://github.com/VsixCommunity/Community.VisualStudio.Toolkit/issues/401
            item.GetItemInfo(out IVsHierarchy hierarchy, out uint itemID, out _);
            if (HierarchyUtilities.TryGetHierarchyProperty(hierarchy, itemID, (int)__VSHPROPID.VSHPROPID_IsNonMemberItem, out bool isNonMemberItem))
            {
                if (isNonMemberItem)
                {
                    // The item is not usually visible. Skip it.
                    return;
                }
            }

            if (predicate(item))
            {
                cancellationToken.ThrowIfCancellationRequested();

                //check for selection for this file
                DocumentView? documentView = null;
                if (item.FullPath is not null)
                {
                    documentView = await VS.Documents.GetDocumentViewAsync(
                        item.FullPath
                        );
                }

                //if the document is selected, put it in the head of the list
                if (documentView is not null)
                {
                    foundItems.Insert(
                        0,
                        item
                        );
                }
                else
                {
                    foundItems.Add(
                        item
                        );
                }
            }

            foreach (var child in item.Children)
            {
                if (child == null)
                {
                    continue;
                }

                await ProcessDownRecursivelyForAsync(
                    foundItems,
                    child,
                    predicate,
                    cancellationToken
                    );
            }
        }

        private sealed class FoundSolutionItems
        {
            public List<SolutionItem> Result
            {
                get;
            }

            public HashSet<SolutionItem> Uniqueness
            {
                get;
            }

            public FoundSolutionItems()
            {
                Result = new List<SolutionItem>();
                Uniqueness = new HashSet<SolutionItem>();
            }

            public void Add(SolutionItem item)
            {
                if (Uniqueness.Contains(item))
                {
                    return;
                }

                Uniqueness.Add(item);
                Result.Add(item);
            }

            public void Insert(int index, SolutionItem item)
            {
                if (Uniqueness.Contains(item))
                {
                    return;
                }

                Uniqueness.Add(item);
                Result.Insert(index, item);
            }
        }

    }

}
