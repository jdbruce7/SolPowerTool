using System.ComponentModel.Composition;

namespace SolPowerTool.App.Interfaces.Views
{
    [InheritedExport]
    public interface IDirtyReadonlyPromptView : IView<IDirtyReadonlyPromptView>
    {
        void Close();
        bool? ShowDialog();
    }
}