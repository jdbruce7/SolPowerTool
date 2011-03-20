using System;
using SolPowerTool.App.Interfaces;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for ProjectDetailView2.xaml
    /// </summary>
    public partial class ProjectDetailView2 : IProjectDetailView
    {
        public ProjectDetailView2()
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

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.Dispose();
            base.OnClosed(e);
        }
    }
}