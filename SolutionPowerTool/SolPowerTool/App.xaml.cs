using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using SolPowerTool.App.Common;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string APPNAME = "Solution Power Tool";
        private MainWindowViewModel vm;


        protected override void OnStartup(StartupEventArgs e)
        {
            ExceptionHandler.Register();

            vm = new MainWindowViewModel();
            vm.Run();
            base.OnStartup(e);
        }


        protected override void OnExit(ExitEventArgs e)
        {
            vm.Dispose();
            base.OnExit(e);
        }
    }
}