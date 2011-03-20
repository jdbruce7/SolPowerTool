using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace SolPowerTool.App.Common
{
    public static class ExceptionHandler
    {
        public static void Register()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            Debug.WriteLine(ex, "Domain Unhandled Exception");
            // TODO: Create EventLog source?
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
    }
}
