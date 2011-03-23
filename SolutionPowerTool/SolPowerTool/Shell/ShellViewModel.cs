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
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Windows.Controls;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Interfaces.Shell;
using SolPowerTool.App.Interfaces.Views;
using SolPowerTool.App.Properties;

namespace SolPowerTool.App.Shell
{
    public class ShellViewModel : ViewModelBase<IShellView>, IShellViewModel
    {
        #region Fields

        private static readonly BuildConfigurationCompare _buildConfigCompare;
        private ObservableCollection<BuildConfigItemFilter> _buildConfigFilters;
        private bool _isBuildConfigFiltered;

        private ICollectionView _projectConfigsView;
        private IEnumerable<BuildConfiguration> _projectConfigurations;
        private BuildConfiguration _selectedConfiguration;
        private Project _selectedProject;
        private Reference _selectedReference;
        private DataGridRowDetailsVisibilityMode _showDetails;
        private bool _showOnlySelected;
        private Solution _solution;
        private string _solutionFilename;

        #endregion


        static ShellViewModel()
        {
            _buildConfigCompare = new BuildConfigurationCompare();
        }

        public ShellViewModel()
        {
            BuildConfigItemFilter.SelectedChanged += BuildConfigItemFilter_SelectedChanged;
            DTOBase.AnyDirtyChanged += DTOBase_AnyDirtyChanged;

            ShowDetails = Settings.Default.ShowDetails;
            SolutionFilename = Settings.Default.SolutionFilename;
            SelectedControlTab = Settings.Default.SelectedControlTab;
            SelectedProjectsTab = Settings.Default.SelectedProjectsTab;

            if (LoadSolutionCommand.CanExecute(null))
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => LoadSolutionCommand.Execute(null)), DispatcherPriority.Background);
        }

        #region Bindings 
        


        public string Title
        {
            get { return Solution != null ? string.Format("{0} - {1}", Solution.Name, App.APPNAME) : App.APPNAME; }
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
                ShowProjectDetailCommand.CanExecute(null);
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

        #region Commands

        private ICommand _aboutBoxCommand;
        private ICommand _checkoutCommand;
        private ICommand _editProjectFileCommand;
        private ICommand _exportViewCommand;
        private ICommand _fixMissingElementsCommand;
        private ICommand _loadSolutionCommand;
        private ICommand _makeWriteableCommand;
        private ICommand _saveChangesCommand;
        private ICommand _selectFileCommand;
        private ICommand _selectProjectsCommand;
        private ICommand _showProjectDetailCommand;
        private ICommand _toggleCACommand;

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
                            RaisePropertyChanged(() => Title);
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
                           new RelayCommand<object>(param => _makeWriteable(), param => _canMakeWriteable()));
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

        public ICommand ExportViewCommand
        {
            get { return _exportViewCommand ?? (_exportViewCommand = new RelayCommand<object>(param => _export())); }
        }

        public ICommand AboutBoxCommand
        {
            get { return _aboutBoxCommand ?? (_aboutBoxCommand = new RelayCommand<object>(param => Container.GetExportedValue<IAboutBoxViewModel>().ShowDialog())); }
        }

        public ICommand CheckOutCommand
        {
            get
            {
                return _checkoutCommand ??
                       (
                           _checkoutCommand = new RelayCommand<object>(param => _checkout()));
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

        public ICommand ShowProjectDetailCommand
        {
            get
            {
                return _showProjectDetailCommand ??
                       (
                           _showProjectDetailCommand = new RelayCommand<object>(
                                                           param => Container.GetExportedValue<IProjectDetailViewModel>().Show(SelectedProject),
                                                           param => SelectedProject != null)
                       );
            }
        }

        #endregion

        #endregion

        #region Overrides

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

        #endregion

        #region Listeners

        private void _solution_DirtyChanged(object sender, EventArgs e)
        {
            _saveChangesCommand.CanExecute(null);
        }

        private void DTOBase_AnyDirtyChanged(object sender, EventArgs e)
        {
            if (sender is Project)
                if (_saveChangesCommand != null) _saveChangesCommand.CanExecute(null);
        }

        private void BuildConfigItemFilter_SelectedChanged(object sender, EventArgs e)
        {
            _applyBuildConfigFilter();
        }

        #endregion

        #region Helpers

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

        private void _export()
        {
            string file = Path.GetTempFileName();
            File.Delete(file);
            file = file + ".csv";
            switch (SelectedProjectsTab)
            {
                case ProjectTabs.Projects:
                    _exportProjects(file);
                    break;
                case ProjectTabs.BuildConfigOutputs:
                    _exportBuildConfigOutputs(file);
                    break;
                case ProjectTabs.BuildConfigCodeAnalysis:
                    _exportBuildConfigCodeAnalysis(file);
                    break;
                case ProjectTabs.References:
                    _exportReferences(file);
                    break;
            }
            Process.Start(file);
        }

        private void _exportProjects(string file)
        {
            using (var sr = new StreamWriter(file, false))
            {
                sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                 {
                                                                     "Name",
                                                                     "Assembly name",
                                                                     "Default namespace",
                                                                     "Output type",
                                                                     "Has pre-build event",
                                                                     "Has post-build event",
                                                                     "File name",
                                                                 }));
                foreach (Project p in Solution.Projects)
                {
                    sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                     {
                                                                         p.ProjectName,
                                                                         p.AssemblyName,
                                                                         p.RootNamespace,
                                                                         p.OutputType,
                                                                         p.HasPreBuildEvent.ToString(),
                                                                         p.HasPostBuildEvent.ToString(),
                                                                         p.ProjectFilename,
                                                                     }));
                }
            }
        }

        private void _exportReferences(string file)
        {
            using (var sr = new StreamWriter(file, false))
            {
                sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                 {
                                                                     "Reference Name",
                                                                     "Project Name",
                                                                     "Include",
                                                                     "Specific version",
                                                                     "Copy local",
                                                                     "Hint path",
                                                                     "File not found",
                                                                 }));
                foreach (Reference r in Solution.DistinctReferences)
                {
                    sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                     {
                                                                         r.Name,
                                                                         r.Project.ProjectName,
                                                                         r.Include,
                                                                         r.SpecificVersion.ToString(),
                                                                         r.Private.ToString(),
                                                                         r.RootedHintPath,
                                                                         (!r.HasFile).ToString(),
                                                                     }));
                }
            }
        }

        private void _exportBuildConfigOutputs(string file)
        {
            using (var sr = new StreamWriter(file, false))
            {
                sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                 {
                                                                     "Project Name",
                                                                     "Configuration Name",
                                                                     "Output path (normalized)",
                                                                     "Output path (actual)",
                                                                     "Irregular",
                                                                 }));
                foreach (BuildConfiguration bc in _projectConfigurations.Where(bc => !bc.IsExcluded))
                {
                    sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                     {
                                                                         bc.Project.ProjectName,
                                                                         bc.Name,
                                                                         bc.NormalizedOutputPath,
                                                                         bc.OutputPath,
                                                                         bc.Project.IrregularOutputPaths.ToString(),
                                                                     }));
                }
            }
        }

        private void _exportBuildConfigCodeAnalysis(string file)
        {
            using (var sr = new StreamWriter(file, false))
            {
                sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                 {
                                                                     "Project Name",
                                                                     "Configuration Name",
                                                                     "Run CA",
                                                                     "Ruleset file",
                                                                     "Ruleset file not found",
                                                                     "Missing elements",
                                                                 }));
                foreach (BuildConfiguration bc in _projectConfigurations.Where(bc => !bc.IsExcluded))
                {
                    sr.WriteLine("\"{0}\"", string.Join("\",\"", new[]
                                                                     {
                                                                         bc.Project.ProjectName,
                                                                         bc.Name,
                                                                         bc.RunCodeAnalysis.ToString(),
                                                                         bc.RootedCodeAnalysisRuleSet,
                                                                         (!bc.HasCodeAnalysisRuleSetFile).ToString(),
                                                                         bc.IsMissingElements.ToString(),
                                                                     }));
                }
            }
        }

        private void _saveChanges()
        {
            // Check for dirty read-only
            IEnumerable<Project> projects = _solution.Projects.Where(p => p.IsReadOnly && p.IsDirty);
            bool allGood = true;
            if (projects.Count() > 0)
            {
                var vm = Container.GetExportedValue<IDirtyReadonlyPromptViewModel>();
                vm.Projects = projects;
                vm.ShowDialog();
                switch (vm.Result)
                {
                    case DirtyReadonlyPromptResults.MakeWriteable:
                        allGood = projects.All(project => project.MakeWriteable());
                        break;
                    case DirtyReadonlyPromptResults.Checkout:
                        allGood = TeamFoundationClient.Checkout(projects.Select(p => p.ProjectFilename));
                        break;
                    case DirtyReadonlyPromptResults.Cancel:
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

        #endregion

        #region Nested type: BuildConfigItemFilter

        public class BuildConfigItemFilter : PropertyChangedBase
        {
            private static readonly List<BuildConfigItemFilter> _instances = new List<BuildConfigItemFilter>();
            private bool _isSelected;

            public BuildConfigItemFilter(BuildConfiguration buildConfiguration)
            {
                Name = buildConfiguration.Name;
                _instances.Add(this);
            }

            public string Name { get; set; }

            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    _isSelected = value;
                    if (SelectedChanged != null)
                        SelectedChanged(this, EventArgs.Empty);
                }
            }

            public static event EventHandler SelectedChanged;
        }

        #endregion

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