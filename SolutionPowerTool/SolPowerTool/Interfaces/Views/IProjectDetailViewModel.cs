using System.ComponentModel.Composition;
using System.Windows.Input;
using SolPowerTool.App.Data;

namespace SolPowerTool.App.Interfaces.Views
{
    [InheritedExport]
    public interface IProjectDetailViewModel : IViewModel<IProjectDetailView>
    {
        void Show(Project selectedProject);
        Project Project { get; set; }
        ICommand ReloadCommand { get; }
        ICommand SaveCommand { get; }
        ICommand CloseCommand { get; }
    }
}