using System.ComponentModel.Composition;

namespace SolPowerTool.App.Interfaces.Shell
{
    public interface IShellView : IView<IShellView>
    {
        void Show();
    }
}