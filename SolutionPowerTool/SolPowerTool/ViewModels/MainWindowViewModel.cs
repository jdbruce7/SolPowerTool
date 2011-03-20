using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Windows.Controls;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Properties;
using SolPowerTool.App.Views;

namespace SolPowerTool.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly BuildConfigurationCompare _buildConfigCompare;
        private ObservableCollection<BuildConfigItemFilter> _buildConfigFilters;
        private ICommand _checkoutCommand;
        private ICommand _editProjectFileCommand;
        private ICommand _fixMissingElementsCommand;
        //private ICommand _filterOutBuildConfigsCommand;
        private bool _isBuildConfigFiltered;
        private ICommand _loadSolutionCommand;
        private ICommand _makeWriteableCommand;
        private ICollectionView _projectConfigsView;
        private IEnumerable<BuildConfiguration> _projectConfigurations;
        private ICommand _saveChangesCommand;
        private ICommand _selectFileCommand;
        private ICommand _selectProjectsCommand;
        private BuildConfiguration _selectedConfiguration;
        private Project _selectedProject;
        private Reference _selectedReference;
        private DataGridRowDetailsVisibilityMode _showDetails;
        private bool _showOnlySelected;
        private ICommand _showProjectDetailCommand;
        private Solution _solution;
        private string _solutionFilename;
        private ICommand _toggleCACommand;

        static MainWindowViewModel()
        {
            _buildConfigCompare = new BuildConfigurationCompare();
        }

        public MainWindowViewModel()
        {
            View = new MainWindowView();
            View.ViewModel = this;

            BuildConfigItemFilter.SelectedChanged += BuildConfigItemFilter_SelectedChanged;
            DTOBase.AnyDirtyChanged += DTOBase_AnyDirtyChanged;

            ShowDetails = Settings.Default.ShowDetails;
            //SolutionFilename = @"D:\Dev\MEFConsoleApp\MEFConsoleApp.sln";
            //SolutionFilename = @"Y:\TFS1\CAR_Audition\Main\Aura.Client.sln";
            SolutionFilename = Settings.Default.SolutionFilename;
            SelectedControlTab = Settings.Default.SelectedControlTab;
            SelectedProjectsTab = Settings.Default.SelectedProjectsTab;

            if (LoadSolutionCommand.CanExecute(null))
                LoadSolutionCommand.Execute(null);
        }

        public MainWindowView View { get; private set; }

        public ICommand SelectFileCommand
        {
            get
            {
                return _selectFileCommand ??
                       (_selectFileCommand = new RelayCommand<object>(param => _selectFile()));
            }
        }

        public ICommand LoadSolutionCommand
        {
            get
            {
                return _loadSolutionCommand ??
                       (_loadSolutionCommand =
                        new RelayCommand<object>(param =>
                                                     {
                                                         Solution = Solution.Parse(SolutionFilename);
                                                         if (_saveChangesCommand != null)
                                                             _saveChangesCommand.CanExecute(null);
                                                     },
                                                 parem => File.Exists(SolutionFilename)));
            }
        }

        public ICommand MakeWriteableCommand
        {
            get
            {
                return _makeWriteableCommand ??
                       (
                           _makeWriteableCommand =
                           new RelayCommand<object>(param => { _makeWriteable(); }, param => _canMakeWriteable()));
            }
        }

        public ICommand SaveChangesCommand
        {
            get
            {
                return _saveChangesCommand ??
                       (
                           _saveChangesCommand =
                           new RelayCommand<object>(param => _saveChanges(),
                                                    param => Solution != null && Solution.IsDirty));
            }
        }

        public ICommand EditProjectFileCommand
        {
            get
            {
                return _editProjectFileCommand
                       ?? (_editProjectFileCommand
                           = new RelayCommand<object>(param =>
                                                          {
                                                              if (SelectedProject != null)
                                                                  Process.Start(SelectedProject.ProjectFilename);
                                                          }));
            }
        }

        public ICommand ToggleCACommand
        {
            get
            {
                return _toggleCACommand ?? (_toggleCACommand = new RelayCommand<object>(
                                                                   param => _toggleCA()));
            }
        }

        public ICommand FixMissingElementsCommand
        {
            get
            {
                return _fixMissingElementsCommand
                       ?? (_fixMissingElementsCommand
                           = new RelayCommand<object>(param =>
                                                          {
                                                              foreach (BuildConfiguration configuration in _projectConfigurations.Where(bc => !bc.IsExcluded && bc.RunCodeAnalysis && bc.IsMissingElements))
                                                                  configuration.IsDirty = true;
                                                          }));
            }
        }

        public Project SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                if (value == _selectedProject) return;
                _selectedProject = value;
                RaisePropertyChanged(() => SelectedProject);
                MakeWriteableCommand.CanExecute(null);
            }
        }

        public BuildConfiguration SelectedConfiguration
        {
            get { return _selectedConfiguration; }
            set
            {
                if (value == _selectedConfiguration) return;
                _selectedConfiguration = value;
                if (value != null)
                    SelectedProject = value.Project;
                RaisePropertyChanged(() => SelectedConfiguration);
            }
        }

        public Reference SelectedReference
        {
            get { return _selectedReference; }
            set
            {
                if (value == _selectedReference) return;
                _selectedReference = value;
                if (value != null)
                    SelectedProject = value.Project;
                RaisePropertyChanged(() => SelectedReference);
            }
        }

        public ControlTabs SelectedControlTab { get; set; }
        public ProjectTabs SelectedProjectsTab { get; set; }

        public ICommand CheckOutCommand
        {
            get
            {
                return _checkoutCommand ??
                       (
                           _checkoutCommand = new RelayCommand<object>(param => _checkout()));
            }
        }

        public ICommand ShowProjectDetailCommand
        {
            get
            {
                return _showProjectDetailCommand ??
                       (
                           _showProjectDetailCommand = new RelayCommand<object>(
                                                           param => ProjectDetailViewModel.ShowProjectDetail(SelectedProject),
                                                           param => SelectedProject != null)
                       );
            }
        }

        public ICollectionView ProjectsView
        {
            get
            {
                if (_solution == null) return null;

                ICollectionView collectionView = CollectionViewSource.GetDefaultView(_solution.Projects);
                collectionView.SortDescriptions.Add(new SortDescription("ProjectName", ListSortDirection.Ascending));
                if (_showOnlySelected)
                    collectionView.Filter = new Predicate<object>(param => !((Project) param).IsSelected);
                return collectionView;
            }
        }

        public ICollectionView ProjectConfigurationsView
        {
            get
            {
                if (_projectConfigsView != null)
                    return _projectConfigsView;
                _projectConfigsView = CollectionViewSource.GetDefaultView(_projectConfigurations);
                if (_projectConfigsView != null)
                {
                    _projectConfigsView.Filter = new Predicate<object>(param => !((BuildConfiguration) param).IsExcluded);
                    _projectConfigsView.SortDescriptions.Add(new SortDescription("Project.ProjectName", ListSortDirection.Ascending));
                    _projectConfigsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    _projectConfigsView.GroupDescriptions.Add(new PropertyGroupDescription("Project"));
                }
                return _projectConfigsView;
            }
        }

        public Solution Solution
        {
            get { return _solution; }
            private set
            {
                if (_solution != null)
                    _solution.DirtyChanged -= _solution_DirtyChanged;
                _solution = value;
                if (_solution != null)
                    _solution.DirtyChanged += _solution_DirtyChanged;
                RaisePropertyChanged(() => Solution);
                RaisePropertyChanged(() => ProjectsView);
                _populateProjectConfigurations();
                RaisePropertyChanged(() => BuildConfigFilters);
            }
        }

        public string SolutionFilename
        {
            get { return _solutionFilename; }
            set
            {
                _solutionFilename = value;
                LoadSolutionCommand.CanExecute(null);
                RaisePropertyChanged(() => SolutionFilename);
            }
        }

        public ObservableCollection<BuildConfigItemFilter> BuildConfigFilters
        {
            get
            {
                if (_solution == null) return null;

                _buildConfigFilters = new ObservableCollection<BuildConfigItemFilter>(_solution.Projects.SelectMany(p => p.BuildConfigurations).Select(
                    b => new BuildConfigItemFilter(b)).Distinct(_buildConfigCompare).OrderBy(b => b.Name));
                return _buildConfigFilters;
            }
        }

        public bool ShowOnlySelected
        {
            get { return _showOnlySelected; }
            set
            {
                if (_showOnlySelected == value)
                    return;
                _showOnlySelected = value;
                RaisePropertyChanged(() => ShowOnlySelected);
                RaisePropertyChanged(() => ProjectsView);
                _populateProjectConfigurations();
            }
        }

        public DataGridRowDetailsVisibilityMode ShowDetails
        {
            get { return _showDetails; }
            set
            {
                _showDetails = value;
                RaisePropertyChanged(() => ShowDetails);
            }
        }

        public ICommand SelectProjectsCommand
        {
            get
            {
                return _selectProjectsCommand
                       ?? (_selectProjectsCommand
                           = new RelayCommand<string>(
                                 param =>
                                     {
                                         switch (param.ToLower())
                                         {
                                             case "configurations":
                                                 _selectProjectWithSelectedBuildConfigs();
                                                 break;
                                             case "selectall":
                                             case "deselectall":
                                                 foreach (Project project in Solution.Projects)
                                                     project.IsSelected = param.ToLower() == "selectall";
                                                 break;
                                             case "invert":
                                                 foreach (Project project in Solution.Projects)
                                                     project.IsSelected = !project.IsSelected;
                                                 break;
                                         }
                                     }));
            }
        }

        public bool IsBuildConfigFiltered
        {
            get { return _isBuildConfigFiltered; }
            set
            {
                _isBuildConfigFiltered = value;
                _applyBuildConfigFilter();
                RaisePropertyChanged(() => IsBuildConfigFiltered);
            }
        }

        private void _toggleCA()
        {
            IEnumerable<BuildConfiguration> includedConfigs = _projectConfigurations.Where(bc => !bc.IsExcluded);
            if (includedConfigs.Any(bc => !bc.RunCodeAnalysis))
                foreach (BuildConfiguration configuration in includedConfigs)
                    configuration.RunCodeAnalysis = true;
            else
                foreach (BuildConfiguration configuration in includedConfigs)
                    configuration.RunCodeAnalysis = false;
        }

        private void _solution_DirtyChanged(object sender, EventArgs e)
        {
            _saveChangesCommand.CanExecute(null);
        }

        private void DTOBase_AnyDirtyChanged(object sender, EventArgs e)
        {
            if (sender is Project)
                if (_saveChangesCommand != null) _saveChangesCommand.CanExecute(null);
        }


        private void _saveChanges()
        {
            // Check for dirty read-only
            IEnumerable<Project> projects = _solution.Projects.Where(p => p.IsReadOnly && p.IsDirty);
            bool allGood = true;
            if (projects.Count() > 0)
            {
                var vm = new DirtyReadonlyPromptViewModel(projects);
                vm.ShowDialog();
                switch (vm.Result)
                {
                    case DirtyReadonlyPromptViewModel.Results.MakeWriteable:
                        allGood = projects.All(project => project.MakeWriteable());
                        break;
                    case DirtyReadonlyPromptViewModel.Results.Checkout:
                        allGood = TeamFoundationClient.Checkout(projects.Select(p => p.ProjectFilename));
                        break;
                    case DirtyReadonlyPromptViewModel.Results.Cancel:
                    default:
                        return;
                }
            }
            foreach (Project project in _solution.Projects.Where(p => p.IsDirty && !p.IsReadOnly))
                project.CommitChanges();
            if (Solution.IsDirty)
                MessageBox.Show("Not all changes were saved.", App.APPNAME, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private bool _canMakeWriteable()
        {
            return SelectedProject != null && SelectedProject.IsReadOnly;
        }

        private void _makeWriteable()
        {
            if (SelectedProject != null)
                SelectedProject.MakeWriteable();
        }

        private void _checkout()
        {
            if (SelectedProject != null)
                TeamFoundationClient.Checkout(SelectedProject.ProjectFilename);
        }


        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                Settings.Default.ShowDetails = ShowDetails;
                Settings.Default.SolutionFilename = SolutionFilename;
                Settings.Default.SelectedControlTab = SelectedControlTab;
                Settings.Default.SelectedProjectsTab = SelectedProjectsTab;

                Settings.Default.Save();
            }
            base.OnDispose(disposing);
        }

        private void _populateProjectConfigurations()
        {
            _projectConfigsView = null;
            _projectConfigurations = _solution != null
                                         ? new ObservableCollection<BuildConfiguration>(
                                               _solution.Projects.SelectMany(p => p.BuildConfigurations).Where(
                                                   bc => !_showOnlySelected || !bc.IsExcluded)
                                               )
                                         : null;
            RaisePropertyChanged(() => ProjectConfigurationsView);
        }

        private void BuildConfigItemFilter_SelectedChanged(object sender, EventArgs e)
        {
            _applyBuildConfigFilter();
        }

        private void _applyBuildConfigFilter()
        {
            IEnumerable<string> enumerable = _buildConfigFilters.Where(b => b.IsSelected).Select(b => b.Name);
            Project.FilterOut = _isBuildConfigFiltered
                                    ? enumerable
                                    : null;
            _populateProjectConfigurations();
        }

        private void _selectProjectWithSelectedBuildConfigs()
        {
            IEnumerable<string> selectedBCs = _buildConfigFilters.Where(b => b.IsSelected).Select(b => b.Name);
            foreach (Project project in
                Solution.Projects.Where(
                    project =>
                    project.BuildConfigurations.Any(buildConfiguration => selectedBCs.Contains(buildConfiguration.Name)))
                )
            {
                project.IsSelected = true;
            }
        }

        public void Run()
        {
            View.Show();
        }

        private void _selectFile()
        {
            var ofd = new OpenFileDialog();
            ofd.AddExtension = true;
            ofd.CheckFileExists = true;
            ofd.DefaultExt = "sln";
            ofd.Filter = "Solution Files (*.sln)|*.sln";

            if (ofd.ShowDialog() == true)
            {
                SolutionFilename = ofd.FileName;
                if (_loadSolutionCommand.CanExecute(null))
                    _loadSolutionCommand.Execute(null);
            }
        }

        #region Nested type: BuildConfigurationCompare

        private class BuildConfigurationCompare : IEqualityComparer<BuildConfigItemFilter>
        {
            #region IEqualityComparer<BuildConfigItemFilter> Members

            public bool Equals(BuildConfigItemFilter x, BuildConfigItemFilter y)
            {
                //Check whether the compared objects reference the same data.
                if (ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null.
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                    return false;

                //Check whether the products' properties are equal.
                return x.Name == y.Name;
            }

            public int GetHashCode(BuildConfigItemFilter buildConfiguration)
            {
                //Check whether the object is null
                if (ReferenceEquals(buildConfiguration, null)) return 0;

                //Get hash code for the Name field if it is not null.
                int hashProductName = buildConfiguration.Name == null ? 0 : buildConfiguration.Name.GetHashCode();

                //Calculate the hash code for the product.
                return hashProductName;
            }

            #endregion
        }

        #endregion
    }
}