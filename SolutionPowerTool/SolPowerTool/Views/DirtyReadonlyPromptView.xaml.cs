using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Threading;
using SolPowerTool.App.Interfaces;
using SolPowerTool.App.Interfaces.Views;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for DirtyReadonlyPromptView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class DirtyReadonlyPromptView : Window, IDirtyReadonlyPromptView
    {
        public DirtyReadonlyPromptView()
        {
            InitializeComponent();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as IViewModel;
            if (vm != null)
            {
                Dispatcher.BeginInvoke(new Action(vm.Dispose), DispatcherPriority.ContextIdle);
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}