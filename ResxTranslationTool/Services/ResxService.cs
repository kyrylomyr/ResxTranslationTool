using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using ResxTranslationTool.Models;

namespace ResxTranslationTool.Services
{
    internal sealed class ResxService
    {
        public const string DEFAULT_TAG = "[translate me]";

        private readonly string _path;

        public ResxService(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("The path value should not be empty.", "path");

            _path = path;
        }

        public string SyncResources(string resourceTag = null)
        {
            checkPath();
            var tag = checkTag(resourceTag);

            // Get all resource files (including resources for different cultures).
            var allFiles = Directory.GetFiles(_path, "*.resx", SearchOption.AllDirectories);

            // Collect all resource files and group them by unique name. Leave only resources with different cultures.
            var resourceFileGroups = allFiles
                .Select(
                    x =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(x);

                        // Extract unique resource name without culture suffix.
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var match = Regex.Match(fileName, @"^(?<name>.*?)(\.[a-z]{2})?$", RegexOptions.IgnoreCase);
                        var name = match.Groups["name"].Value;

                        return new
                               {
                                   // ReSharper disable once AssignNullToNotNullAttribute
                                   Base = Path.Combine(Path.GetDirectoryName(x), name),
                                   FileName = x,
                                   ResourceName = name
                               };
                    })
                .GroupBy(x => x.Base)
                .Where(x => x.Count() > 1)
                .ToList();

            var logBuilder = new StringBuilder();

            // Analyze every multi-culture resource file.
            foreach (var resourceFileGroup in resourceFileGroups)
            {
                // Order by file name so the base file without culture suffix will appear at the beginning.
                var resourceFiles = resourceFileGroup.OrderByDescending(x => x.FileName).ToList();
                var baseFile = resourceFiles[0];

                // Separate base file from the files for other cultures.
                resourceFiles.RemoveAt(0);

                // Collect all resource entries from the base file. Take only string value entries.
                Dictionary<string, string> allBaseEntries;
                using (var resx = new ResXResourceReader(baseFile.FileName))
                {
                    resx.UseResXDataNodes = true;
                    allBaseEntries = (from DictionaryEntry entry in resx
                                      let value = getStringNodeValue(entry)
                                      where value != null
                                      select new
                                             {
                                                 Id = (string)entry.Key,
                                                 Value = value
                                             })
                        .ToDictionary(x => x.Id, x => x.Value);
                }

                // Go through each multi-culture resource file and update it according to the entries from the base file.
                foreach (var resourceFile in resourceFiles)
                {
                    logBuilder.AppendFormat("> {0}\r\n", resourceFile.FileName);

                    // Get all entries of any type from the file.
                    Dictionary<string, object> allEntries;
                    using (var resx = new ResXResourceReader(resourceFile.FileName))
                    {
                        resx.UseResXDataNodes = true;
                        allEntries = resx.Cast<DictionaryEntry>()
                                         .ToDictionary(x => (string)x.Key, x => x.Value);
                    }

                    var changed = false;

                    // Remove string value entries which are not present in the base file.
                    var stringNodeKeys =
                        allEntries.Where(x => isStringNode((ResXDataNode)x.Value)).Select(x => x.Key).ToList();
                    foreach (var key in stringNodeKeys)
                    {
                        if (allBaseEntries.ContainsKey(key))
                            continue;
                        
                        allEntries.Remove(key);
                        changed = true;

                        logBuilder.AppendFormat("   - '{0}'\r\n", key);
                    }

                    // Copy all string value entries from the base file which are not present in the current
                    // resource file. Prefix them with the tag.
                    foreach (var baseEntry in allBaseEntries)
                    {
                        if (allEntries.ContainsKey(baseEntry.Key))
                            continue;

                        var newValue = string.Format("{0} {1}", tag, baseEntry.Value);

                        allEntries.Add(baseEntry.Key, newValue);
                        changed = true;

                        logBuilder.AppendFormat("   + '{0}' = '{1}'\r\n", baseEntry.Key, newValue);
                    }

                    // Update resource file.
                    if (changed)
                    {
                        // Creation of writer flushes the file content! That's why previously we have read all entries.
                        using (var resx = new ResXResourceWriter(resourceFile.FileName))
                        {
                            foreach (var entry in allEntries)
                            {
                                resx.AddResource(entry.Key, entry.Value);
                            }
                        }
                    }
                }
            }

            return logBuilder.ToString();
        }

        public IEnumerable<Translation> GetResources(string fileMask, string resourceTag = null)
        {
            if (string.IsNullOrEmpty(fileMask))
                throw new ArgumentException("The file mask value should not be empty.", "fileMask");

            checkPath();
            var tag = checkTag(resourceTag);

            // Find all files in the specified directory with the specified file mask.
            var files = Directory.GetFiles(_path, fileMask, SearchOption.AllDirectories);

            // Collect tagged resources from each found file.
            var translations = new List<Translation>();
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
            checkPath();

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

        private void checkPath()
        {
            if (!Directory.Exists(_path))
                throw new DirectoryNotFoundException(string.Format("The directory '{0}' doesn't exist.", _path));
        }

        private static string checkTag(string resourceTag)
        {
            return string.IsNullOrEmpty(resourceTag) ? DEFAULT_TAG : resourceTag;
        }

        private static string getStringNodeValue(DictionaryEntry entry)
        {
            var node = (ResXDataNode)entry.Value;
            return isStringNode(node) ? (string)node.GetValue((ITypeResolutionService)null) : null;
        }

        private static bool isStringNode(ResXDataNode node)
        {
            return node.FileRef == null &&
                   node.GetValueTypeName((ITypeResolutionService)null) == typeof(string).AssemblyQualifiedName;
        }
    }
}