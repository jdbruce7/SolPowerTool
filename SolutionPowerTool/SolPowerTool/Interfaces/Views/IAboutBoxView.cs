using System.ComponentModel.Composition;

namespace SolPowerTool.App.Interfaces.Views
{
    [InheritedExport]
    public interface IAboutBoxView : IView<IAboutBoxView>
    {
        bool? ShowDialog();
    }
}