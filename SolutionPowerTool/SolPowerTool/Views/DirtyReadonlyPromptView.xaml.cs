using System.Windows;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for DirtyReadonlyPromptView.xaml
    /// </summary>
    public partial class DirtyReadonlyPromptView : Window
    {
        public DirtyReadonlyPromptView()
        {
            InitializeComponent();
        }

        public DirtyReadonlyPromptViewModel ViewModel
        {
            get { return (DirtyReadonlyPromptViewModel) DataContext; }
            set { DataContext = value; }
        }
    }
}