using EnvDTE;
using Extension.Helper;
using Extension.UI.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using System.IO;
using System.Threading.Tasks;

namespace Extension.Commands
{
    public static class CSharpProcessor
    {
        public static async Task<bool> ProcessDocumentAsync(
            DocumentView documentView,
            AddResourceResult result,
            string namespaceName
            )
        {
            var resourceFileInfo = new FileInfo(
                result.Group.ResxList[0].FilePath
                );
            var resourceFileNameWithoutExtension = resourceFileInfo.Name.Substring(0, resourceFileInfo.Name.Length - resourceFileInfo.Extension.Length);

            var componentModel = await VS.Services.GetComponentModelAsync();
            if (componentModel == null)
            {
                return false;
            }

            for(var i = 0; i < 5; i++)
            {
                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                if (workspace == null)
                {
                    return false;
                }

                var (subjectDocument, subjectSyntaxRoot) = await workspace.GetDocumentAndSyntaxRootAsync(
                    documentView.FilePath
                    );
                if (subjectDocument == null || subjectSyntaxRoot == null)
                {
                    return false;
                }

                var syntaxTree = await subjectDocument.GetSyntaxTreeAsync();
                if (syntaxTree is null)
                {
                    return false;
                }

                var root = await syntaxTree.GetRootAsync();
                if (root is null)
                {
                    return false;
                }

                var selectedSpan = documentView.TextView.Selection.SelectedSpans[0];
                var token = root.FindToken(selectedSpan.Start);
                if (token == default)
                {
                    return false;
                }

                var fullyQualifiedClause = $"{namespaceName}.{resourceFileNameWithoutExtension}.{result.ResourceName}";

                var modifiedDocument = await ReplaceTokenCompletelyAsync(
                    subjectDocument,
                    token,
                    fullyQualifiedClause
                    );
                if (modifiedDocument is null)
                {
                    return false;
                }

                //modifiedDocument = await AddUsingDirectiveAsync(
                //    modifiedDocument,
                //    namespaceName
                //    );

                var modifiedWorkspace = subjectDocument.Project.Solution.Workspace;
                if (workspace.TryApplyChanges(modifiedDocument.Project.Solution))
                {
                    break;
                }
            }

            return true;
        }

        public static async Task<Microsoft.CodeAnalysis.Document> ReplaceTokenCompletelyAsync(
            Microsoft.CodeAnalysis.Document document,
            SyntaxToken token,
            string newTokenText
            )
        {
            // Validate inputs
            if (document == null || token == default(SyntaxToken))
                return document;

            if (!IsStringLiteral(token))
                return document;

            // Get the span of the entire token
            var tokenSpan = token.Span;

            // Create text change to replace the entire token
            var textChange = new TextChange(tokenSpan, newTokenText);

            // Apply the change to the document
            var sourceText = await document.GetTextAsync();
            var newSourceText = sourceText.WithChanges(textChange);

            return document.WithText(newSourceText);
        }

        public static async Task<Microsoft.CodeAnalysis.Document?> TryReplaceStringLiteralContentAsync(
            Microsoft.CodeAnalysis.Document document,
            SyntaxToken token,
            string newContent)
        {
            try
            {
                // Validate inputs
                if (document == null || token == default)
                    return null;

                if (!IsStringLiteral(token))
                    return null;

                // Get current source text
                var sourceText = await document.GetTextAsync();
                var tokenSpan = token.Span;

                // Validate span
                if (tokenSpan.End > sourceText.Length)
                    return null;

                // Get the original text including quotes
                var originalFullText = token.Text;

                // Create new text with preserved format
                var newFullText = CreateStringLiteralText(newContent, originalFullText);

                // Create and apply text change
                var textChange = new TextChange(tokenSpan, newFullText);
                var newSourceText = sourceText.WithChanges(textChange);
                var newDocument = document.WithText(newSourceText);

                return newDocument;
            }
            catch (Exception ex)
            {
                //todo log
                System.Diagnostics.Debug.WriteLine($"Failed to replace string literal: {ex.Message}");
            }

            return null;
        }

        private static string CreateStringLiteralText(string newContent, string originalFullText)
        {
            // Determine the type of string literal and preserve the format
            if (originalFullText.StartsWith("\"\"\""))
            {
                // Multi-line raw string
                return $"\"\"\"{newContent}\"\"\"";
            }
            else if (originalFullText.StartsWith("u8\"\"\""))
            {
                // UTF-8 multi-line raw string
                return $"u8\"\"\"{newContent}\"\"\"";
            }
            else if (originalFullText.StartsWith("@\""))
            {
                // Verbatim string
                return $"@\"{newContent}\"";
            }
            else if (originalFullText.StartsWith("u8@\""))
            {
                // UTF-8 verbatim string
                return $"u8@\"{newContent}\"";
            }
            else if (originalFullText.StartsWith("u8\""))
            {
                // UTF-8 regular string
                return $"u8\"{EscapeStringContent(newContent)}\"";
            }
            else if (originalFullText.StartsWith("\""))
            {
                // Regular string
                return $"\"{EscapeStringContent(newContent)}\"";
            }

            return newContent; // Fallback
        }

        private static bool IsStringLiteral(SyntaxToken token)
        {
            return token.Kind() switch
            {
                SyntaxKind.StringLiteralToken => true,
                SyntaxKind.StringLiteralExpression => true,
                SyntaxKind.MultiLineRawStringLiteralToken => true,
                SyntaxKind.SingleLineRawStringLiteralToken => true,
                SyntaxKind.Utf8MultiLineRawStringLiteralToken => true,
                SyntaxKind.Utf8SingleLineRawStringLiteralToken => true,
                SyntaxKind.Utf8StringLiteralExpression => true,
                SyntaxKind.Utf8StringLiteralToken => true,
                _ => false
            };
        }

        private static string EscapeStringContent(string content)
        {
            return content
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                ;
        }


        //private static async Task<Document> AddUsingDirectiveAsync(
        //    Document document,
        //    string namespaceName
        //    )
        //{
        //    var root = await document.GetSyntaxRootAsync() as CompilationUnitSyntax;
        //    if (root == null)
        //        return document;

        //    // Check if there are namespace-level using directives
        //    var namespaceDeclarations = root.DescendantNodes()
        //        .OfType<NamespaceDeclarationSyntax>()
        //        .Where(ns => ns.Usings.Any())
        //        .ToList();

        //    if (namespaceDeclarations.Any())
        //    {
        //        // Add to the first namespace that has using directives
        //        var firstNamespaceWithUsings = namespaceDeclarations.First();
        //        return await AddUsingToNamespaceAsync(document, firstNamespaceWithUsings, namespaceName);
        //    }
        //    else if (root.Usings.Any())
        //    {
        //        // Add to compilation unit level
        //        return await AddUsingToCompilationUnitAsync(document, namespaceName);
        //    }
        //    else if (root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Any())
        //    {
        //        // No existing usings but there are namespaces - add to first namespace
        //        var firstNamespace = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
        //        return await AddUsingToNamespaceAsync(document, firstNamespace, namespaceName);
        //    }
        //    else
        //    {
        //        // Add to compilation unit level as fallback
        //        return await AddUsingToCompilationUnitAsync(document, namespaceName);
        //    }
        //}

        //private static async Task<Document> AddUsingToCompilationUnitAsync(Document document, string namespaceName)
        //{
        //    var root = await document.GetSyntaxRootAsync() as CompilationUnitSyntax;
        //    if (root == null)
        //        return document;

        //    // Check if already exists
        //    if (root.Usings.Any(u => u.Name.ToString() == namespaceName))
        //        return document;

        //    // Create the new using directive with proper formatting
        //    var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(" " + namespaceName))
        //        .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
        //        ;

        //    // Find insert position
        //    var usings = root.Usings;
        //    int insertIndex = FindInsertPosition(usings, namespaceName);

        //    SyntaxList<UsingDirectiveSyntax> newUsings;

        //    if (insertIndex == usings.Count)
        //    {
        //        // Inserting at the end
        //        newUsings = usings.Add(newUsing);
        //    }
        //    else
        //    {
        //        // Inserting in the middle - preserve trailing trivia of previous using
        //        newUsings = usings.Insert(insertIndex, newUsing);
        //    }

        //    var newRoot = root.WithUsings(newUsings);
        //    return document.WithSyntaxRoot(newRoot);
        //}

        //private static async Task<Document> AddUsingToNamespaceAsync(Document document, NamespaceDeclarationSyntax namespaceDeclaration, string namespaceName)
        //{
        //    // Check if already exists
        //    if (namespaceDeclaration.Usings.Any(u => u.Name.ToString() == namespaceName))
        //        return document;

        //    // Create the new using directive with proper line breaks
        //    var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(" " + namespaceName))
        //        .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
        //        ;

        //    // Find insert position within namespace usings
        //    var usings = namespaceDeclaration.Usings;
        //    int insertIndex = FindInsertPosition(usings, namespaceName);

        //    var newUsings = usings.Insert(insertIndex, newUsing);
        //    var newNamespace = namespaceDeclaration.WithUsings(newUsings);

        //    // Replace the namespace in the document
        //    var root = await document.GetSyntaxRootAsync();
        //    var newRoot = root.ReplaceNode(namespaceDeclaration, newNamespace);

        //    return document.WithSyntaxRoot(newRoot);
        //}
        //private static int FindInsertPosition(SyntaxList<UsingDirectiveSyntax> usings, string newNamespace)
        //{
        //    for (int i = 0; i < usings.Count; i++)
        //    {
        //        var existingNamespace = usings[i].Name.ToString();
        //        if (string.Compare(newNamespace, existingNamespace, StringComparison.Ordinal) < 0)
        //            return i;
        //    }
        //    return usings.Count;
        //}
    }
}

