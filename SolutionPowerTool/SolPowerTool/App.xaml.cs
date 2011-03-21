using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Properties;
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
            var splash = new SplashView();

            splash.SetVersion(Assembly.GetEntryAssembly().GetName().Version);
            splash.SetMessage("Loading...");
            splash.Show();

            vm = new MainWindowViewModel();
            vm.Run();

            if (string.IsNullOrWhiteSpace(Settings.Default.CodeAnalysisRuleDirectories))
            {
                Settings.Default.CodeAnalysisRuleDirectories = BuildConfiguration.CodeAnalysisRuleDirectories;
                Settings.Default.CodeAnalysisRuleSetDirectories = BuildConfiguration.CodeAnalysisRuleSetDirectories;
                splash.SetMessage("Click here to continue.");
            }
            else
            {
                splash.SetMessage("Welcome to Solution Power Tool.");
                ThreadPool.QueueUserWorkItem(o =>
                                                 {
                                                     Thread.Sleep(1300);
                                                     splash.Dispatcher.BeginInvoke(new Action(splash.CloseView));
                                                 });
            }
            base.OnStartup(e);
        }


        protected override void OnExit(ExitEventArgs e)
        {
            vm.Dispose();
            base.OnExit(e);
        }
    }
}