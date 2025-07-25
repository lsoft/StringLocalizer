using Extension.Commands;
using Extension.Helper;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Extension
{
    public sealed class ResxGroup
    {
        private readonly List<Resx> _resxList;

        public string FolderPath
        {
            get;
        }

        public SolutionItem Project
        {
            get;
        }

        public IReadOnlyList<Resx> ResxList => _resxList;

        public string UIDescription
        {
            get
            {
                var cultures = string.Join(", ", _resxList.Select(r => r.GetCultureName()));
                return $"{cultures} ({FolderPath})";
            }
        }

        public ResxGroup(
            string folderPath,
            SolutionItem project,
            IEnumerable<SolutionItem> items
            )
        {
            if (folderPath is null)
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            FolderPath = folderPath;
            Project = project;
            _resxList = new();
            foreach (var item in items)
            {
                if (!GetCultureFromResxName(item.FullPath, out var culture))
                {
                    continue;
                }

                if (culture is null)
                {
                    if (_resxList.Any(r => r.IsNeutralCulture))
                    {
                        continue;
                    }
                }

                _resxList.Add(
                    new Resx(
                        project,
                        item,
                        culture
                        )
                    );
            }
        }

        private static bool GetCultureFromResxName(
            string filePath,
            out CultureInfo? culture
            )
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var parts = fileName.Split('.');

            // Стандартный формат: "BaseName.Culture.resx" (например, "Resources.ru.resx")
            if (parts.Length == 0)
            {
                culture = null;
                return false;
            }
            if (parts.Length == 1)
            {
                culture = null;
                return true;
            }

            var cultureName = parts[parts.Length - 1]; // Последняя часть перед .resx
            try
            {
                // Проверяем валидность культуры через .NET
                culture = CultureInfo.GetCultureInfo(cultureName);
                return true;
            }
            catch (CultureNotFoundException)
            {
                // Игнорируем некорректные имена
                culture = null;
                return false;
            }
        }

        public bool CheckForExistingKey(
            string resourceName
            )
        {
            if (_resxList.Any(r => r.CheckForExistingKey(resourceName)))
            {
                return true;
            }

            return false;
        }

        public async Task RebuildDesignerForResxFileAsync()
        {
            await ResxDesignerRebuilder.RebuildDesignerForResxFileAsync(ResxList[0].FilePath);
        }

        public async System.Threading.Tasks.Task<string> TryDetermineTargetNamespaceAsync()
        {
            var targetNamespace = await Project.TryDetermineTargetNamespaceAsync(
                ResxList[0].SolutionItem.FullPath
                );
            return targetNamespace;
        }
    }
}
