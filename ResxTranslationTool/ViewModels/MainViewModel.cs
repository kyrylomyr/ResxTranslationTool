using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using ResxTranslationTool.Models;

namespace ResxTranslationTool.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private string _solutionFileName;
        private List<Translation> _translations;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            OpenSolutionFileCommand = new DelegateCommand(openSolutionFileCommand);
            ScanSolutionCommand = new DelegateCommand(scanSolution);
            UpdateSolutionCommand = new DelegateCommand(updateSolution);
        }

        public ICommand OpenSolutionFileCommand { get; private set; }

        public ICommand ScanSolutionCommand { get; private set; }

        public ICommand UpdateSolutionCommand { get; private set; }

        public string SolutionFileName
        {
            get { return _solutionFileName; }
            set { SetProperty(ref _solutionFileName, value); }
        }

        public List<Translation> Translations
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
                             DefaultExt = ".sln",
                             Filter = "Visual Studio Solution File (.sln)|*.sln",
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
            if (string.IsNullOrEmpty(SolutionFileName) && openSolutionFile() == false)
            {
                return;
            }

            var solutionPath = Path.GetDirectoryName(SolutionFileName);
            if (string.IsNullOrEmpty(solutionPath))
                throw new ApplicationException("The solution path is empty or null.");

            var translations = new List<Translation>();

            var files = Directory.GetFiles(solutionPath, "*.resx", SearchOption.AllDirectories);
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
                              node.GetValueTypeName((ITypeResolutionService)null) == typeof(string).AssemblyQualifiedName
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

            Translations = translations;
        }

        private void updateSolution()
        {
            if (string.IsNullOrEmpty(SolutionFileName) && openSolutionFile() == false)
            {
                return;
            }

            var solutionPath = Path.GetDirectoryName(SolutionFileName);
        }
    }
}