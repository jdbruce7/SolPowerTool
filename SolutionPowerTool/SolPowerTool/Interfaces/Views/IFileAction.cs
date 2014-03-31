namespace SolPowerTool.App.Interfaces.Views
{
    public interface IFileAction
    {
        bool MakeWriteable();
        string Filename { get; }
    }
}