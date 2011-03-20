using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Interfaces;
using SolPowerTool.App.Views;

namespace SolPowerTool.App.ViewModels
{
    public class ProjectDetailViewModel : ViewModelBase, IProjectDetailViewModel
    {
        private readonly Project _project;
        private static List<ProjectDetailViewModel> _instances;

        static ProjectDetailViewModel()
        {
            _instances = new List<ProjectDetailViewModel>();
        }

        public static ProjectDetailViewModel ShowProjectDetail(Project project)
        {
            ProjectDetailViewModel projectDetailViewModel = _instances.Where(vm => vm._project == project).FirstOrDefault();
            if (projectDetailViewModel == null)
                projectDetailViewModel = new ProjectDetailViewModel(project);
            else
            {
                projectDetailViewModel.View.Focus();
                projectDetailViewModel.View.Activate();
            }
            return projectDetailViewModel;
        }

        private ProjectDetailViewModel(Project project)
        {
            View = new ProjectDetailView2 { ViewModel = this };
            _project = project;
            _project.DirtyChanged += new EventHandler((sender, args) => _saveCommand.CanExecute(null));
            _instances.Add(this);

            View.Show();
        }

        public IProjectDetailView View { get; private set; }

        public Project Project { get { return _project; } }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                _instances.Remove(this);
            base.OnDispose(disposing);
        }

        private ICommand _reloadCommand;
        public ICommand ReloadCommand
        {
            get
            {
                return _reloadCommand ?? (_reloadCommand
                                          = new RelayCommand<object>(param => Project.Reload()));
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand ?? (_saveCommand
                                        = new RelayCommand<object>(_save, param => Project.IsDirty));
            }
        }

        private void _save(object param)
        {
            // Check for dirty read-only
            IEnumerable<Project> projects = new[] {Project};
            bool allGood = true;
            var vm = new DirtyReadonlyPromptViewModel(projects);
            vm.ShowDialog();
            switch (vm.Result)
            {
                case DirtyReadonlyPromptViewModel.Results.MakeWriteable:
                    allGood = projects.All(project => project.MakeWriteable());
                    break;
                case DirtyReadonlyPromptViewModel.Results.Checkout:
                    allGood = TeamFoundationClient.Checkout(projects.Select(p=>p.ProjectFilename));
                    break;
                case DirtyReadonlyPromptViewModel.Results.Cancel:
                default:
                    return;
            }
            if (!allGood)
                return;
            if (!Project.IsReadOnly)
                Project.CommitChanges();
        }

        private ICommand _closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                return _closeCommand ?? (_closeCommand
                                         = new RelayCommand<object>(param => View.Close()));
            }
        }
    }
}
