using System.Windows;
using System.Windows.Input;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for AboutBoxView.xaml
    /// </summary>
    public partial class AboutBoxView : Window
    {
        public AboutBoxView()
        {
            InitializeComponent();
        }

        public AboutBoxViewModel ViewModel
        {
            get { return (AboutBoxViewModel) DataContext; }
            set { DataContext = value; }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}