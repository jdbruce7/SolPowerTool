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
    /// Interaction logic for ProjectDetailView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    //[Export(typeof(IProjectDetailView))]
    public partial class ProjectDetailView : Window, IProjectDetailView
    {
        public ProjectDetailView()
        {
            InitializeComponent();
        }

        #region IProjectDetailView Members

        public IProjectDetailViewModel ViewModel
        {
            get { return (ProjectDetailViewModel) DataContext; }
            set { DataContext = value; }
        }

        #endregion

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