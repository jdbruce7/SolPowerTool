using System.Windows;
using System.Windows.Input;
using SolPowerTool.App.Interfaces.Shell;

namespace SolPowerTool.App.Shell
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window, IShellView 
    {
        public ShellView()
        {
            InitializeComponent();
        }

        public IShellViewModel ViewModel
        {
            get { return (IShellViewModel) DataContext; }
            set { DataContext = value; }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.ShowProjectDetailCommand.CanExecute(null))
                ViewModel.ShowProjectDetailCommand.Execute(null);
        }
    }
}