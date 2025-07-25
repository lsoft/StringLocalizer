using Extension.UI.ViewModels;
using MSXML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Extension.Commands
{
    public static class XamlProcessor
    {
        public static bool ProcessDocument(
            DocumentView xamlDocumentView,
            AddResourceResult result,
            string targetNamespace
            )
        {
            var resourceFileInfo = new FileInfo(
                result.Group.ResxList[0].FilePath
                );
            var resourceFileNameWithoutExtension = resourceFileInfo.Name.Substring(0, resourceFileInfo.Name.Length - resourceFileInfo.Extension.Length);
            var resourceName = result.ResourceName;

            var xamlDocument = new XmlDocument();
            xamlDocument.LoadXml(xamlDocumentView.TextBuffer.CurrentSnapshot.GetText());

            if (xamlDocument.ChildNodes.Count == 0)
            {
                return false;
            }

            var attrs = xamlDocument.ChildNodes[0]?.Attributes;
            if (attrs is null || attrs.Count == 0)
            {
                return false;
            }

            var attributeName = GetExistingAttributeName(
                attrs,
                targetNamespace
                );
            if (attributeName is null)
            {
                //we need to add new xmlns attribute
                attributeName = DetermineUniqueAttributeName(
                    attrs,
                    resourceFileNameWithoutExtension
                    );
                
                var attributeBody = $"""xmlns:{attributeName}="clr-namespace:{targetNamespace}" """;
                AddNewAttribute(
                    xamlDocumentView,
                    attrs[attrs.Count - 1].OuterXml,
                    attributeBody
                    );
            }

            var bindingClause = $$"""{x:Static {{attributeName}}:{{resourceFileNameWithoutExtension}}.{{resourceName}}}""";

            var selectedSpan = xamlDocumentView.TextView.Selection.SelectedSpans[0];
            using (var edit = xamlDocumentView.TextBuffer.CreateEdit())
            {
                edit.Replace(selectedSpan, bindingClause);
                edit.Apply();
            }

            return true;
        }

        private static void AddNewAttribute(
            DocumentView xamlDocumentView,
            string previousAttributeBody,
            string attributeBody
            )
        {
            using (var edit = xamlDocumentView.TextBuffer.CreateEdit())
            {
                var text = edit.Snapshot.GetText();
                var index = text.IndexOf(previousAttributeBody);
                if (index <= 0)
                {
                    return;
                }

                edit.Replace(
                    index,
                    previousAttributeBody.Length,
                    previousAttributeBody + " " + attributeBody
                    );
                edit.Apply();
            }
        }

        private static string DetermineUniqueAttributeName(
            XmlAttributeCollection attrs,
            string resourceFileName
            )
        {
            var probeName = new FileInfo(resourceFileName).Name.ToLower();

            var index = 0;
            while (true)
            {
                foreach (XmlAttribute attr in attrs)
                {
                    if (attr.LocalName == probeName)
                    {
                        goto nextCycle;
                    }
                }
                return probeName;

            nextCycle:
                probeName = new FileInfo(resourceFileName).Name.ToLower() + ++index;
            }
        }

        private static string? GetExistingAttributeName(
            XmlAttributeCollection attrs,
            string namespaceValue
            )
        {
            var fullnv = $"clr-namespace:{namespaceValue}";

            foreach (XmlAttribute attr in attrs)
            {
                if (attr.Value != fullnv)
                {
                    continue;
                }

                return attr.LocalName;
            }

            return null;
        }

    }
}
