using EnvDTE;
using EnvDTE80;
using System;
using System.IO;
using System.Threading.Tasks;
using VSLangProj;
using Project = EnvDTE.Project;

namespace Extension.Commands
{
    public static class ResxDesignerRebuilder
    {
        public static async Task<bool> RebuildDesignerForResxFileAsync(string resxFilePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

            try
            {
                // Find the project item
                var projectItem = FindProjectItem(dte, resxFilePath);
                if (projectItem == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Project item not found: {resxFilePath}");
                    return false;
                }

                // Get custom tool
                var customTool = GetCustomTool(projectItem);
                System.Diagnostics.Debug.WriteLine($"Custom tool for {projectItem.Name}: {customTool}");

                // Run custom tool
                (projectItem.Object as VSProjectItem).RunCustomTool();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to rebuild designer: {ex.Message}");
                return false;
            }
        }

        private static ProjectItem FindProjectItem(
            DTE2 dte,
            string filePath
            )
        {
            // Try to find by iterating through projects
            foreach (Project project in dte.Solution.Projects)
            {
                var item = FindProjectItemInProject(project, filePath);
                if (item != null)
                    return item;
            }
            return null;
        }

        private static ProjectItem FindProjectItemInProject(Project project, string filePath)
        {
            if (project.ProjectItems == null)
                return null;

            foreach (ProjectItem item in project.ProjectItems)
            {
                var found = FindProjectItemRecursive(item, filePath);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static ProjectItem FindProjectItemRecursive(ProjectItem item, string filePath)
        {
            try
            {
                if (item.FileNames[0] != null &&
                    string.Equals(item.FileNames[0], filePath, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }

                if (item.ProjectItems != null)
                {
                    foreach (ProjectItem childItem in item.ProjectItems)
                    {
                        var found = FindProjectItemRecursive(childItem, filePath);
                        if (found != null)
                            return found;
                    }
                }
            }
            catch
            {
                // Some items might not have file names, ignore them
            }

            return null;
        }

        private static string GetCustomTool(ProjectItem item)
        {
            try
            {
                Properties properties = item.Properties;
                Property customToolProperty = properties.Item("CustomTool");
                return customToolProperty?.Value?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
