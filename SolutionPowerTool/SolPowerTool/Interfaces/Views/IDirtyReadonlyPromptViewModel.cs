using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using SolPowerTool.App.Data;

namespace SolPowerTool.App.Interfaces.Views
{
    [InheritedExport]
    public interface IDirtyReadonlyPromptViewModel : IViewModel<IDirtyReadonlyPromptView>
    {
        DirtyReadonlyPromptResults Result { get; }
        IEnumerable<IFileAction> Projects { get; set; }
        ICommand MakeWriteableCommand { get; }
        ICommand CheckoutCommand { get; }
        ICommand CancelCommand { get; }
        bool? ShowDialog();
    }
}