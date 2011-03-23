using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SolPowerTool.App.Interfaces;
using SolPowerTool.App.Interfaces.Views;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Views
{
    /// <summary>
    /// Interaction logic for AboutBoxView.xaml
    /// </summary>
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AboutBoxView : Window, IAboutBoxView
    {
        public AboutBoxView()
        {
            InitializeComponent();
        }

        #region Listeners

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key==Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
            e.Handled = true;
        }

        #endregion

        #region Overrides

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as IViewModel;
            if (vm != null)
            {
                Dispatcher.BeginInvoke(new Action(vm.Dispose), DispatcherPriority.ContextIdle);
                e.Cancel = true;
            }
            base.OnClosing(e);
        }

        #endregion
    }
}