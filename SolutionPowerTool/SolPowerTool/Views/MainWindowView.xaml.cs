using System.Windows;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindowView.xaml
    /// </summary>
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        public MainWindowViewModel ViewModel
        {
            get { return (MainWindowViewModel) DataContext; }
            set { DataContext = value; }
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.ShowProjectDetailCommand.CanExecute(null))
                ViewModel.ShowProjectDetailCommand.Execute(null);
        }
    }
}