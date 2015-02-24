using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using ResxTranslationTool.Models;

namespace ResxTranslationTool.Services
{
    internal sealed class ResxService
    {
        public const string DEFAULT_TAG = "[translate me]";

        private readonly string _path;
        private readonly string _fileMask;

        public ResxService(string path, string fileMask)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("The path value should not be empty.", "path");

            if (string.IsNullOrEmpty(fileMask))
                throw new ArgumentException("The file mask value should not be empty.", "fileMask");

            _path = path;
            _fileMask = fileMask;
        }

        public IEnumerable<Translation> GetResources(string translationTag = null)
        {
            if (!Directory.Exists(_path))
                throw new DirectoryNotFoundException(string.Format("The directory '{0}' doesn't exist.", _path));

            var tag = string.IsNullOrEmpty(translationTag) ? DEFAULT_TAG : translationTag;

            var translations = new List<Translation>();

            // Find all files in the specified directory with the specified file mask.
            var files = Directory.GetFiles(_path, _fileMask, SearchOption.AllDirectories);

            // Collect tagged resources from each found file.
            foreach (var file in files)
            {
                // Get relative path.
                var relativeFileName = Path.GetFullPath(file).Substring(_path.Length + 1);

                using (var resx = new ResXResourceReader(file))
                {
                    resx.UseResXDataNodes = true;

                    translations.AddRange(
                        from DictionaryEntry entry in resx
                        let node = (ResXDataNode)entry.Value
                        where node.FileRef == null &&
                              node.GetValueTypeName((ITypeResolutionService)null) ==
                              typeof(string).AssemblyQualifiedName
                        let value = (string)node.GetValue((ITypeResolutionService)null)
                        where value.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0
                        select new Translation
                               {
                                   Id = (string)entry.Key,
                                   FileName = relativeFileName,
                                   OriginalText = value,
                                   Comment = node.Comment
                               });
                }
            }

            return translations;
        }

        public void UpdateResources(IEnumerable<Translation> translations)
        {
            // Group all translation by resource file name.
            var grouppedTranslations =
                translations.Where(x => !string.IsNullOrEmpty(x.TranslatedText)).GroupBy(x => x.FileName);

            foreach (var translationGroup in grouppedTranslations)
            {
                var fileName = Path.Combine(_path, translationGroup.Key);

                // Read all existing resources.
                Dictionary<string, object> allEntries;
                using (var resx = new ResXResourceReader(fileName))
                {
                    resx.UseResXDataNodes = true;
                    allEntries = resx.Cast<DictionaryEntry>().ToDictionary(x => (string)x.Key, x => x.Value);
                }

                // Update resources with translated values.
                foreach (var translation in translationGroup)
                {
                    allEntries[translation.Id] = translation.TranslatedText;
                }

                // Save all resource entries.
                using (var resx = new ResXResourceWriter(fileName))
                {
                    foreach (var entry in allEntries)
                    {
                        resx.AddResource(entry.Key, entry.Value);
                    }
                }
            }
        }
    }
}