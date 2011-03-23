using System.ComponentModel.Composition;

namespace SolPowerTool.App.Interfaces.Shell
{
    [InheritedExport]
    public interface IShellView : IView<IShellView>
    {
        void Show();
    }
}