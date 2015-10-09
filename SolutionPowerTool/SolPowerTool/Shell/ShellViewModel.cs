using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
using MessageBox = System.Windows.MessageBox;

namespace SolPowerTool.App.Shell
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IShellViewModel))]
    public class ShellViewModel : ViewModelBase<IShellView>, IShellViewModel
    {
        #region Fields

        private static readonly BuildConfigurationCompare _buildConfigCompare;
        private ObservableCollection<BuildConfigItemFilter> _buildConfigFilters;
        private string _busyMessage;
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
            _selectedTargetFrameworkVersion = "v4.6";
            SolutionFilename = Settings.Default.SolutionFilename;
            SelectedControlTab = (ControlTabs)Settings.Default.SelectedControlTab;
            SelectedProjectsTab = (ProjectTabs)Settings.Default.SelectedProjectsTab;

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
                    collectionView.Filter = new Predicate<object>(param => !((Project)param).IsSelected);
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
                    _projectConfigsView.Filter = new Predicate<object>(param => !((BuildConfiguration)param).IsExcluded);
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

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                _isBusy = value;
                RaisePropertyChanged(() => IsBusy);
            }
        }

        public string BusyMessage
        {
            get { return _busyMessage; }
            private set
            {
                _busyMessage = value;
                RaisePropertyChanged(() => BusyMessage);
                IsBusy = true;
            }
        }

        #region Commands

        private ICommand _aboutBoxCommand;
        private ICommand _checkoutCommand;
        private ICommand _editProjectFileCommand;
        private ICommand _exportViewCommand;
        private ICommand _fixMissingElementsCommand;
        private bool _isBusy;
        private ICommand _loadSolutionCommand;
        private ICommand _makeWriteableCommand;
        private ICommand _saveChangesCommand;
        private ICommand _selectFileCommand;
        private ICommand _selectProjectsCommand;
        private ICommand _showProjectDetailCommand;
        private ICommand _toggleCACommand;
        private string _selectedTargetFrameworkVersion;

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
                        new RelayCommand<object>(
                            _beginLoadSolution,
                            parem => File.Exists(SolutionFilename)));
            }
        }

        private void _beginLoadSolution(object param)
        {
            BusyMessage = "Loading...";
            ThreadPool.QueueUserWorkItem(o =>
                {
                    _loadSolution();
                    IsBusy = false;
                });
        }
        private void _loadSolution()
        {
            Solution = Solution.Parse(SolutionFilename);
            if (_saveChangesCommand != null)
                _saveChangesCommand.CanExecute(null);
            RaisePropertyChanged(() => Title);
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

        public ICommand UpgradeProjectsCommand
        {
            get { return new RelayCommand(_upgradeProjects); }
        }

        private void _upgradeProjects()
        {
            foreach (var project in _solution.Projects.Where(project => project.IsSelected))// && project.TargetFrameworkVersion != SelectedTargetFrameworkVersion
            {
                project.TargetFrameworkVersion = SelectedTargetFrameworkVersion;

            }
        }



        public ICommand AddMissingProjectReferencedProjectsCommand
        {
            get
            {
                return new RelayCommand(_addMissingProjectReferencedProjects, _hasMissingProjectReferencedProjects); new NotImplementedException();
            }
        }

        public string SelectedTargetFrameworkVersion
        {
            get { return _selectedTargetFrameworkVersion; }
            set { _selectedTargetFrameworkVersion = value; }
        }

        public string[] AvailableTargetFrameworkVersions => new[]
            {
                "v4.6",
                "v4.5.2",
                "v4.5.1",
                "v4.5",
                "v4.0",
                "v4.0 Client Profile",
            };

        private void _addMissingProjectReferencedProjects()
        {
            // Check for dirty read-only
            bool allGood = true;
            if ((File.GetAttributes(SolutionFilename) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {

                var vm = Container.GetExportedValue<IDirtyReadonlyPromptViewModel>();
                vm.Projects = new[] { Solution };
                vm.ShowDialog();
                switch (vm.Result)
                {
                    case DirtyReadonlyPromptResults.MakeWriteable:
                        allGood = vm.Projects.All(project => project.MakeWriteable());
                        break;
                    case DirtyReadonlyPromptResults.Checkout:
                        allGood = TeamFoundationClient.Checkout(vm.Projects.Select(p => p.Filename));
                        break;
                    case DirtyReadonlyPromptResults.Cancel:
                    default:
                        return;
                }
            }
            if (!allGood)
                return;

            BusyMessage = "Adding missing projects..";
            Task.Factory.StartNew(() =>
                {
                    Solution solution = Solution;
                    do
                    {
                        List<ProjectReference> missingReferences = new List<ProjectReference>();
                        foreach (var project in solution.Projects.Where(p => p.HasMissingProjectReferences))
                        {
                            var missingProjectReferences = project.ProjectReferences.Where(pr => pr.IsNotInSolution);
                            foreach (var projectReference in missingProjectReferences)
                            {
                                var found =
                                    missingReferences.FirstOrDefault(
                                        mr => string.Compare(mr.RootedPath, projectReference.RootedPath, StringComparison.InvariantCultureIgnoreCase) == 0);
                                if (found != null)
                                {
                                    if (projectReference.ProjectGuid != found.ProjectGuid)
                                        throw new ApplicationException("Mismatched project guid");
                                    if (string.Compare(projectReference.Name, found.Name, StringComparison.InvariantCultureIgnoreCase) != 0)
                                        throw new ApplicationException("Mismatched project name");
                                }
                                else
                                {
                                    missingReferences.Add(projectReference);
                                }
                            }
                        }
                        var configs = solution.Projects.SelectMany(p => p.BuildConfigurations).Select(bc => new { bc.Configuration, bc.Platform }).Distinct();

                        /*
             * Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Services", "Services", "{0D439F87-0D86-4C64-A238-B9582749E55C}"
             * EndProject
             * Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "PwC.Aura.Server.Replication.Provider", "Client\AuraReplication\PwC.Aura.Server.Replication.Provider\PwC.Aura.Server.Replication.Provider.csproj", "{6D6DC9C3-744D-478B-806C-751BDE529681}"
             * EndProject
             */
                        StringBuilder projectSection = new StringBuilder();
                        Folder solPowerFolder = solution.Folders
                                                        .FirstOrDefault(f => string.Compare(f.Name, Folder.SOLPOWERFOLDER, StringComparison.InvariantCultureIgnoreCase) == 0);

                        if (solPowerFolder == null)
                        {
                            solPowerFolder = new Folder(Folder.SOLPOWERFOLDER, Guid.NewGuid());
                            projectSection.AppendFormat(@"Project(""{0:b}"") = ""{1}"", ""{2}"", ""{3:b}""" + Environment.NewLine,
                                                        Elements.Project.FolderTypeID,
                                                        solPowerFolder.Name,
                                                        solPowerFolder.Name,
                                                        solPowerFolder.Guid);
                            projectSection.AppendLine("EndProject");
                        }
                        foreach (var missingReference in missingReferences)
                        {
                            projectSection.AppendFormat(@"Project(""{0:b}"") = ""{1}"", ""{2}"", ""{3:b}""" + Environment.NewLine,
                                                        Elements.Project.ProjectTypeID,
                                                        missingReference.Name,
                                                        _relativePath(Solution.SolutionFilename, missingReference.RootedPath),
                                                        missingReference.ProjectGuid);
                            projectSection.AppendLine("EndProject");
                        }

                        /*
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Debug|Any CPU.Build.0 = Debug|Any CPU
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Debug|x86.ActiveCfg = Debug|x86
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Debug|x86.Build.0 = Debug|x86
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Release|Any CPU.ActiveCfg = Release|Any CPU
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Release|Any CPU.Build.0 = Release|Any CPU
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Release|x86.ActiveCfg = Release|x86
                    {1D0DA165-7A0C-448D-8632-3638C870197A}.Release|x86.Build.0 = Release|x86
             */
                        StringBuilder configSection = new StringBuilder();
                        foreach (var missingReference in missingReferences)
                            foreach (var config in configs)
                                foreach (var s in new[] { "ActiveCfg", "Build.0" })
                                    configSection.AppendFormat("\t\t{0:b}.{1}|{2}.{3} = {1}|{2}" + Environment.NewLine,
                                                               missingReference.ProjectGuid,
                                                               config.Configuration,
                                                               config.Platform,
                                                               s);

                        StringBuilder nestedProjectSection = new StringBuilder();
                        foreach (var missingReference in missingReferences)
                            nestedProjectSection.AppendFormat("\t\t{0:b} = {1:b}" + Environment.NewLine, missingReference.ProjectGuid, solPowerFolder.Guid);

                        var backupFilename = solution.SolutionFilename + ".backup";
                        if (File.Exists(backupFilename))
                        {
                            File.SetAttributes(backupFilename, File.GetAttributes(backupFilename) & ~FileAttributes.ReadOnly);
                            File.Delete(backupFilename);
                        }
                        File.Move(SolutionFilename, backupFilename);
                        bool goodwrite = false;
                        using (var reader = new StreamReader(backupFilename))
                        using (var writer = new StreamWriter(SolutionFilename))
                        {
                            var section = FileSections.PreProjects;
                            while (reader.Peek() >= 0)
                            {
                                string line = reader.ReadLine();
                                Debug.Assert(line != null);
                                switch (section)
                                {
                                    case FileSections.PreProjects:
                                        // read until we reach begining of Projects list.
                                        if (line.StartsWith("Project(\""))
                                        {
                                            do
                                            {
                                                writer.WriteLine(line);
                                                line = reader.ReadLine();
                                            } while (string.Compare(line.Trim(), "EndProject", StringComparison.InvariantCultureIgnoreCase) != 0);
                                            section = FileSections.Projects;
                                        }
                                        break;
                                    case FileSections.Projects:
                                        // read until we reach end of projects list.
                                        if (line.StartsWith("Project(\""))
                                        {
                                            do
                                            {
                                                writer.WriteLine(line);
                                                line = reader.ReadLine();
                                            } while (string.Compare(line.Trim(), "EndProject", StringComparison.InvariantCultureIgnoreCase) != 0);
                                        }
                                        else
                                        {
                                            // insert missing projects.
                                            writer.Write(projectSection.ToString());
                                            section = FileSections.PreConfigs;
                                        }
                                        break;
                                    case FileSections.PreConfigs:
                                        // read until we get to GlobalSection(ProjectConfigurationPlatforms) = postSolution
                                        if (
                                            string.Compare(line.Trim(), "GlobalSection(ProjectConfigurationPlatforms) = postSolution", StringComparison.InvariantCultureIgnoreCase) ==
                                            0)
                                            section = FileSections.Configs;
                                        break;
                                    case FileSections.Configs:
                                        // read until we reach the end of configs
                                        if (string.Compare(line.Trim(), "EndGlobalSection", StringComparison.InvariantCultureIgnoreCase) == 0)
                                        {
                                            writer.Write(configSection.ToString());
                                            section = FileSections.PreNestedProjects;
                                        }
                                        break;
                                    case FileSections.PreNestedProjects:
                                        // read until GlobalSection(NestedProjects) = preSolution
                                        if (string.Compare(line.Trim(), "GlobalSection(NestedProjects) = preSolution", StringComparison.InvariantCultureIgnoreCase) == 0)
                                            section = FileSections.NestedProjects;
                                        else if (string.Compare(line.Trim(), "EndGlobal", StringComparison.InvariantCultureIgnoreCase) == 0)
                                        {
                                            writer.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
                                            writer.Write(nestedProjectSection.ToString());
                                            writer.WriteLine("\tEndGlobalSection");
                                            section = FileSections.PostInserts;
                                            goodwrite = true;
                                        }
                                        break;
                                    case FileSections.NestedProjects:
                                        // read until end of nested projects
                                        if (string.Compare(line.Trim(), "EndGlobalSection", StringComparison.InvariantCultureIgnoreCase) == 0)
                                        {
                                            writer.Write(nestedProjectSection.ToString());
                                            section = FileSections.PostInserts;
                                        }
                                        break;
                                    case FileSections.PostInserts:
                                        goodwrite = true;
                                        break;
                                }
                                writer.WriteLine(line);
                            }
                        }

                        if (!goodwrite)
                            throw new ApplicationException("Didn't get through all the solution sections.");

                        solution = Solution.Parse(SolutionFilename);
                        if (solution.Projects.All(p => !p.HasMissingProjectReferences))
                            break;
                    } while (true);

                    _loadSolution();
                    IsBusy = false;
                });

        }

        private enum FileSections
        {
            PreProjects, Projects, PreConfigs, Configs, PreNestedProjects, NestedProjects, PostInserts,
        }

        private string _relativePath(string a, string b)
        {
            var x = new Uri(a);
            var y = new Uri(b);
            return Uri.UnescapeDataString(x.MakeRelativeUri(y).ToString().Replace('/', '\\'));
        }

        private bool _hasMissingProjectReferencedProjects()
        {
            if (Solution == null || Solution.Projects == null)
                return false;
            return Solution.Projects.Any(p => p.HasMissingProjectReferences);
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
                Settings.Default.SelectedControlTab = (int)SelectedControlTab;
                Settings.Default.SelectedProjectsTab = (int)SelectedProjectsTab;

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
            BusyMessage = "Exporting...";
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
            IsBusy = false;
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
            else
                MessageBox.Show("Chagnes saved.", App.APPNAME, MessageBoxButton.OK, MessageBoxImage.Information);
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