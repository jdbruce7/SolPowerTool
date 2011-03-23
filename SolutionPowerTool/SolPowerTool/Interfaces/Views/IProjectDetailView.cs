namespace SolPowerTool.App.Interfaces.Views
{
    public interface IProjectDetailView : IView<IProjectDetailView>
    {
        void Show();
        bool Focus();
        bool Activate();
    }
}