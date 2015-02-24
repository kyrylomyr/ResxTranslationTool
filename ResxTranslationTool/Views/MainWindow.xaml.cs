using System.Diagnostics;
using System.Windows.Navigation;
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
    }
}
