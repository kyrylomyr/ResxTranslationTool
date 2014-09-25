using System.Windows.Input;

namespace ResxTranslationTool.ViewModels
{
    public class MainViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            // OpenSolutionFileCommand = new DelegateCommand(onOpenSolutionFileCommand);
        }

        public ICommand OpenSolutionFileCommand { get; private set; }

        public string SolutionFileName { get; set; }

        private void onOpenSolutionFileCommand()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
                         {
                             DefaultExt = ".sln",
                             Filter = "Visual Studio Solution File (.sln)|*.sln",
                             CheckFileExists = true
                         };

            if (dialog.ShowDialog() == true)
            {
                SolutionFileName = dialog.FileName;
            }
        }
    }
}