using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Interfaces.Views;

namespace SolPowerTool.App.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ProjectDetailViewModel : ViewModelBase<IProjectDetailView>, IProjectDetailViewModel
    {
        #region Fields

        private static readonly List<ProjectDetailViewModel> _instances;
        private Project _project;

        #endregion

        static ProjectDetailViewModel()
        {
            _instances = new List<ProjectDetailViewModel>();
        }

        private ProjectDetailViewModel()
        {
            _instances.Add(this);
        }

        #region Bindings

        public Project Project
        {
            get { return _project; }
            set
            {
                if (value == _project) return;
                _project = value;
                if (_project != null)
                    _project.DirtyChanged += (sender, args) => _saveCommand.CanExecute(null);
            }
        }

        #region Commands

        private ICommand _closeCommand;
        private ICommand _reloadCommand;
        private ICommand _saveCommand;

        public ICommand ReloadCommand
        {
            get
            {
                return _reloadCommand ?? (_reloadCommand
                                          = new RelayCommand<object>(param => { if (Project != null) Project.Reload(); }));
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand ?? (_saveCommand
                                        = new RelayCommand<object>(_save, param => Project != null && Project.IsDirty));
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return _closeCommand ?? (_closeCommand
                                         = new RelayCommand<object>(param => Dispose() /*View.Close()*/));
            }
        }

        #endregion

        #endregion

        #region IProjectDetailViewModel Members

        public void Show(Project project)
        {
            ProjectDetailViewModel projectDetailViewModel = _instances.Where(vm => vm._project == project).FirstOrDefault();
            if (projectDetailViewModel == null)
            {
                _project = project;
                View.Show();
            }
            else
            {
                projectDetailViewModel.View.Focus();
                projectDetailViewModel.View.Activate();
                Dispose();
            }
        }

        #endregion

        #region Overrides

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                _instances.Remove(this);
            base.OnDispose(disposing);
        }

        #endregion

        #region Helpers

        private void _save(object param)
        {
            // Check for dirty read-only
            bool allGood;
            var vm = Container.GetExportedValue<IDirtyReadonlyPromptViewModel>();
            vm.Projects = new[] {Project};
            vm.ShowDialog();
            switch (vm.Result)
            {
                case DirtyReadonlyPromptResults.MakeWriteable:
                    allGood = vm.Projects.All(project => project.MakeWriteable());
                    break;
                case DirtyReadonlyPromptResults.Checkout:
                    allGood = TeamFoundationClient.Checkout(vm.Projects.Select(p => p.ProjectFilename));
                    break;
                case DirtyReadonlyPromptResults.Cancel:
                default:
                    return;
            }
            if (!allGood)
                return;
            if (!Project.IsReadOnly)
                Project.CommitChanges();
        }

        #endregion
    }
}