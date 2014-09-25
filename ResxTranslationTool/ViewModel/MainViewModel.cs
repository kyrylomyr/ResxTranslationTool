using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace ResxTranslationTool.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            ExitCommand = new RelayCommand(() =>
                                           {
                                               Application.Current.Shutdown();
                                           });
        }

        public ICommand ExitCommand { get; private set; }
    }
}