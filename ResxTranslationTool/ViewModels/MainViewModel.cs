using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using ResxTranslationTool.Models;

namespace ResxTranslationTool.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private const string DEFAULT_RESOURCE_FILE_MASK = "*.resx";

        private string _solutionFileName;
        private ObservableCollection<Translation> _translations;
        private string _resourceFileMask;
        private string _translationFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            OpenSolutionFileCommand = new DelegateCommand(openSolutionFileCommand);
            ScanSolutionCommand = new DelegateCommand(scanSolution);
            UpdateSolutionCommand = new DelegateCommand(updateSolution);
            SaveTranslationFileCommand = new DelegateCommand(saveTranslationFile);
            OpenTranslationFileCommand = new DelegateCommand(openTranslationFile);

            ResourceFileMask = "*.da.resx";
            Translations = new ObservableCollection<Translation>();
        }

        public ICommand OpenSolutionFileCommand { get; private set; }

        public ICommand ScanSolutionCommand { get; private set; }

        public ICommand UpdateSolutionCommand { get; private set; }

        public ICommand SaveTranslationFileCommand { get; private set; }

        public ICommand OpenTranslationFileCommand { get; private set; }

        public string SolutionFileName
        {
            get { return _solutionFileName; }
            set { SetProperty(ref _solutionFileName, value); }
        }

        public string ResourceFileMask
        {
            get { return _resourceFileMask; }
            set { SetProperty(ref _resourceFileMask, value); }
        }

        public string TranslationFileName
        {
            get { return _translationFileName; }
            set { SetProperty(ref _translationFileName, value); }
        }

        public ObservableCollection<Translation> Translations
        {
            get { return _translations; }
            set { SetProperty(ref _translations, value); }
        }

        private void openSolutionFileCommand()
        {
            openSolutionFile();
        }

        private bool? openSolutionFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
                         {
                             Filter = "Visual Studio Solution File|*.sln",
                             CheckFileExists = true
                         };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                SolutionFileName = dialog.FileName;
            }

            return result;
        }

        private void scanSolution()
        {
            if (string.IsNullOrEmpty(SolutionFileName) && openSolutionFile() != true)
            {
                return;
            }

            if (string.IsNullOrEmpty(ResourceFileMask))
                ResourceFileMask = DEFAULT_RESOURCE_FILE_MASK;

            var solutionPath = Path.GetDirectoryName(SolutionFileName);
            if (string.IsNullOrEmpty(solutionPath))
                throw new ApplicationException("The solution path is empty or null.");

            var translations = new List<Translation>();

            var files = Directory.GetFiles(solutionPath, ResourceFileMask, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                // Get relative path.
                var relativeFileName = Path.GetFullPath(file).Substring(solutionPath.Length + 1);

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
                        where value.IndexOf("[translate me]", StringComparison.OrdinalIgnoreCase) >= 0
                        select new Translation
                               {
                                   Id = (string)entry.Key,
                                   FileName = relativeFileName,
                                   OriginalText = value
                               });
                }
            }

            Translations = new ObservableCollection<Translation>(translations);
        }

        private void updateSolution()
        {
            if (string.IsNullOrEmpty(SolutionFileName) && openSolutionFile() != true)
            {
                return;
            }

            var solutionPath = Path.GetDirectoryName(SolutionFileName);
            if (string.IsNullOrEmpty(solutionPath))
                throw new ApplicationException("The solution path is empty or null.");

            var translationGroups =
                Translations.Where(x => !string.IsNullOrEmpty(x.TranslatedText)).GroupBy(x => x.FileName);

            foreach (var translationGroup in translationGroups)
            {
                var fileName = Path.Combine(solutionPath, translationGroup.Key);

                // Read all existing resources.
                Dictionary<string, object> allEntries;
                using (var resx = new ResXResourceReader(fileName))
                {
                    resx.UseResXDataNodes = true;
                    allEntries = resx.Cast<DictionaryEntry>().ToDictionary(x => (string)x.Key, x => x.Value);
                }

                // Update resources with translated texts.
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

            MessageBox.Show(
                "Solution resources updated successfully.", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void saveTranslationFile()
        {
            if (string.IsNullOrEmpty(TranslationFileName))
            {
                // Select file name.
                var dialog = new Microsoft.Win32.SaveFileDialog
                             {
                                 Filter = "XML File|*.xml",
                                 OverwritePrompt = true,
                                 CheckPathExists = true,
                                 AddExtension = true
                             };

                if (dialog.ShowDialog() != true)
                    return;

                TranslationFileName = dialog.FileName;
            }

            // Save.
            using (var writer = new StreamWriter(TranslationFileName))
            {
                var solution = new Solution { Translations = Translations.ToList() };
                var serializer = new XmlSerializer(typeof(Solution));
                serializer.Serialize(writer, solution);
            }

            MessageBox.Show("File saved successfully.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void openTranslationFile()
        {
            // Select file name.
            var dialog = new Microsoft.Win32.OpenFileDialog
                         {
                             Filter = "XML File|*.xml",
                             CheckFileExists = true
                         };

            if (dialog.ShowDialog() != true)
                return;

            // Read.
            using (var reader = new StreamReader(dialog.FileName))
            {
                var serializer = new XmlSerializer(typeof(Solution));

                try
                {
                    var solution = (Solution)serializer.Deserialize(reader);
                    Translations = new ObservableCollection<Translation>(solution.Translations);
                    TranslationFileName = dialog.FileName;
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show(
                        "The translation file has incorrect data.",
                        "Open error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}