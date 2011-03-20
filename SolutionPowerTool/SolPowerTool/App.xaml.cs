using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            vm = new MainWindowViewModel();
            vm.Run();
            base.OnStartup(e);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            Debug.WriteLine(ex, "Domain Unhandled Exception");
            //string exs = ex.ToString();
            //if (exs.Length > 32000)
            //    exs = exs.Substring(0, 32000);
            //EventLog.WriteEntry(APPNAME, exs, EventLogEntryType.Error);
            MessageBox.Show(ex.Message, "Unhandled Exception");
        }

        private static void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is NotImplementedException)
            {
                MessageBox.Show("This feature is not yet implemented.", "Under Construction", MessageBoxButton.OK,
                                MessageBoxImage.Hand);
                e.Handled = true;
            }
            else
            {
                Debug.WriteLine(e.Exception, "Dispatcher Unhandled Exception");
                //string exs = e.Exception.ToString();
                //if (exs.Length > 32000)
                //    exs = exs.Substring(0, 32000);
                //EventLog.WriteEntry(APPNAME, exs, EventLogEntryType.Error);
                
                Exception exception = e.Exception is TargetInvocationException ? e.Exception.InnerException : e.Exception;
                const string errorMessageFormat = "{0}: {1}\nSee Windows Application Log for details.\n\nDo you want to continue running this application?";
                string errorMessage = string.Format(errorMessageFormat, exception.GetType().Name, exception.Message);
                e.Handled = (MessageBox.Show(errorMessage, "Dispatcher Unhandled Exception", MessageBoxButton.YesNo) == MessageBoxResult.Yes);
                if (!e.Handled)
                    Environment.Exit(-1);
            }
        }
        protected override void OnExit(ExitEventArgs e)
        {
            vm.Dispose();
            base.OnExit(e);
        }
    }
}