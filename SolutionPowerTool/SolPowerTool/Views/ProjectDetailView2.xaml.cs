using System;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using SolPowerTool.App.Interfaces;
using SolPowerTool.App.Interfaces.Views;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for ProjectDetailView2.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IProjectDetailView))]
    public partial class ProjectDetailView2 : IProjectDetailView
    {
        public ProjectDetailView2()
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