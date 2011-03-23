using System;

namespace SolPowerTool.App.Interfaces
{
    public interface IViewModel : IDisposable
    {
    }

    public interface IViewModel<out TView> : IViewModel where TView : IView<TView>
    {
        TView View { get; }
    }
}