using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using Microsoft.Practices.ServiceLocation;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Interfaces.Shell;
using SolPowerTool.App.Properties;

namespace SolPowerTool.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string APPNAME = "Solution Power Tool";


        protected override void OnStartup(StartupEventArgs e)
        {
            ExceptionHandler.Register();
            var splash = new SplashView();

            splash.SetVersion(Assembly.GetEntryAssembly().GetName().Version);
            splash.SetMessage("Loading...");
            if (Debugger.IsAttached)
                splash.Topmost = false;
            splash.Show();

            new Bootstrapper().Run();

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
            ServiceLocator.Current.GetInstance<IShellViewModel>().Dispose();
            base.OnExit(e);
        }
    }
}