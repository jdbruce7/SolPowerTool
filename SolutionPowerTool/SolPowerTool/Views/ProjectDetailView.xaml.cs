using System;
using System.Windows;
using SolPowerTool.App.Interfaces;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for ProjectDetailView.xaml
    /// </summary>
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

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.Dispose();
            base.OnClosed(e);
        }
    }
}