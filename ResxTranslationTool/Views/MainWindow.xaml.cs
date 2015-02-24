using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using ResxTranslationTool.Models;
using ResxTranslationTool.ViewModels;

namespace ResxTranslationTool.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }

        private void homePageRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void TranslationRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = (DataGridRow)sender;
            var translation = (Translation)row.DataContext;

            if (string.IsNullOrEmpty(translation.TranslatedText))
            {
                translation.TranslatedText = translation.OriginalText;
            }
        }
    }
}
