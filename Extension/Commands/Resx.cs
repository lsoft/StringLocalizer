using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace Extension
{
    public sealed class Resx
    {
        public SolutionItem Project
        {
            get;
        }

        public SolutionItem SolutionItem
        {
            get;
        }

        public CultureInfo? Culture
        {
            get;
        }

        public string FilePath => SolutionItem.FullPath;

        public bool IsNeutralCulture => Culture is null;

        public Resx(
            SolutionItem project,
            SolutionItem solutionItem,
            CultureInfo? culture
            )
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (solutionItem is null)
            {
                throw new ArgumentNullException(nameof(solutionItem));
            }

            Project = project;
            SolutionItem = solutionItem;
            Culture = culture;
        }

        public string GetCultureName()
        {
            if (IsNeutralCulture)
            {
                return "neutral";
            }

            return Culture.Name;
        }

        public bool CheckForExistingKey(string resourceName)
        {
            using var reader = new ResXResourceReader(FilePath);
            foreach (DictionaryEntry pair in reader)
            {
                if ((string)pair.Key == resourceName)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddNewResource(
            string resourceName,
            string neutralComment,
            string resourceText
            )
        {
            var existingResources = new List<ResXDataNode>();

            using var reader = new ResXResourceReader(FilePath);
            reader.UseResXDataNodes = true;
            foreach (DictionaryEntry pair in reader)
            {
                var resxNode = pair.Value as ResXDataNode;
                //(string)resxNode.GetValue((System.ComponentModel.Design.ITypeResolutionService)null)
                existingResources.Add(
                    resxNode
                    );
            }

            using var writer = new ResXResourceWriter(FilePath);

            foreach (var existingResource in existingResources)
            {
                writer.AddResource(
                    existingResource
                    );
            }


            var node = new ResXDataNode(resourceName, resourceText);
            if (IsNeutralCulture)
            {
                node.Comment = neutralComment;
            }

            writer.AddResource(
                node
                );
        }
    }
}
