using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Microsoft.Windows.Controls;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Shell;

namespace SolPowerTool.App.Interfaces.Shell
{
    public interface IShellViewModel : IViewModel<IShellView>
    {
        ICommand ShowProjectDetailCommand { get; }
        string Title { get; }
        Project SelectedProject { get; set; }
        BuildConfiguration SelectedConfiguration { get; set; }
        Reference SelectedReference { get; set; }
        ControlTabs SelectedControlTab { get; set; }
        ProjectTabs SelectedProjectsTab { get; set; }
        ICollectionView ProjectsView { get; }
        ICollectionView ProjectConfigurationsView { get; }
        Solution Solution { get; }
        string SolutionFilename { get; set; }
        ObservableCollection<ShellViewModel.BuildConfigItemFilter> BuildConfigFilters { get; }
        bool ShowOnlySelected { get; set; }
        DataGridRowDetailsVisibilityMode ShowDetails { get; set; }
        bool IsBuildConfigFiltered { get; set; }
        ICommand SelectFileCommand { get; }
        ICommand LoadSolutionCommand { get; }
        ICommand MakeWriteableCommand { get; }
        ICommand SaveChangesCommand { get; }
        ICommand EditProjectFileCommand { get; }
        ICommand ToggleCACommand { get; }
        ICommand FixMissingElementsCommand { get; }
        ICommand ExportViewCommand { get; }
        ICommand AboutBoxCommand { get; }
        ICommand CheckOutCommand { get; }
        ICommand SelectProjectsCommand { get; }
        bool IsBusy { get; }
        string BusyMessage { get; }
    }
}