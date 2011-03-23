using System.ComponentModel.Composition;
using System.Windows.Input;

namespace SolPowerTool.App.Interfaces.Views
{
    [InheritedExport]
    public interface IAboutBoxViewModel : IViewModel<IAboutBoxView>
    {
        bool? ShowDialog();
        string Title { get; }
        string ProductName { get; }
        string Version { get; }
        string Copyright { get; }
        string CompanyName { get; }
        string Description { get; }
        string AssemblyTitle { get; }
        string AssemblyVersion { get; }
        string AssemblyDescription { get; }
        string AssemblyProduct { get; }
        string AssemblyCopyright { get; }
        string AssemblyCompany { get; }
        ICommand ShowWarrantyCommand { get; }
        ICommand ShowCopyrightCommand { get; }
    }
}