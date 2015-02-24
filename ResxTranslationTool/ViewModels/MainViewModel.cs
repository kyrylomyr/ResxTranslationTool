using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.Win32;
using ResxTranslationTool.Models;
using ResxTranslationTool.Services;

namespace ResxTranslationTool.ViewModels
{
    internal sealed class MainViewModel : BindableBase
    {
        private const string DEFAULT_RESOURCE_FILE_MASK = "*.resx";

        private string _solutionFileName;
        private ObservableCollection<Translation> _translations;
        private string _resourceFileMask;
        private string _translationFileName;
        private string _tag;

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

            ResourceFileMask = "*.de.resx";
            Tag = ResxService.DEFAULT_TAG;
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

        public string Tag
        {
            get { return _tag; }
            set { SetProperty(ref _tag, value); }
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
            var dialog = new OpenFileDialog
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
                return;

            if (string.IsNullOrEmpty(ResourceFileMask))
                ResourceFileMask = DEFAULT_RESOURCE_FILE_MASK;

            var service = new ResxService(getSolutionPath(), ResourceFileMask);

            Translations = new ObservableCollection<Translation>(service.GetTaggedResources(Tag));
        }

        private void updateSolution()
        {
            if (string.IsNullOrEmpty(SolutionFileName) && openSolutionFile() != true)
                return;

            var service = new ResxService(getSolutionPath(), ResourceFileMask);
            service.UpdateResources(Translations);

            MessageBox.Show(
                "Solution resources updated successfully.", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void saveTranslationFile()
        {
            if (string.IsNullOrEmpty(TranslationFileName))
            {
                // Select file name.
                var dialog = new SaveFileDialog
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
            var dialog = new OpenFileDialog
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

        private string getSolutionPath()
        {
            var solutionPath = Path.GetDirectoryName(SolutionFileName);
            if (string.IsNullOrEmpty(solutionPath))
                throw new ApplicationException("The solution path is empty or null.");

            return solutionPath;
        }
    }
}