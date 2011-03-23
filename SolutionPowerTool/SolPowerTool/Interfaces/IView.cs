namespace SolPowerTool.App.Interfaces
{
    public interface IView
    {
        object DataContext { get; set; }
    }

    public interface IView<in TView> : IView where TView : IView<TView>
    {
    }
}