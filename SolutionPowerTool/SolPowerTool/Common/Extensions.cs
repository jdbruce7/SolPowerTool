using System.Windows;
using SolPowerTool.App.Interfaces;

namespace SolPowerTool.App.Common
{
    public static class Extensions
    {
        public static void SetViewModel<TViewModel, TView>(this TView me, TViewModel viewModel)
            where TViewModel : IViewModel
            where TView : IView<TView>
        {
            var element = me as FrameworkElement;
            if (element != null) element.DataContext = viewModel;
        }

        public static TViewModel GetViewModel<TViewModel, TView>(this TView me)
            where TViewModel : class, IViewModel<TView>
            where TView : IView<TView>
        {
            var element = me as FrameworkElement;
            return element == null ? null : element.DataContext as TViewModel;
        }
    }
}