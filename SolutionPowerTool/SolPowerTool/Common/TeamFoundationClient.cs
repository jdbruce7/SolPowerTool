using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Windows;

namespace SolPowerTool.App.Common
{
    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
    public static class TeamFoundationClient
    {
        private static readonly string TF_EXE;

        #region Cmd file definitions

        private const string ERRORLEVELCHECK =
            @"SET ERR=%ERRORLEVEL%
if %ERR% NEQ 0 ( 
    SET SCRIPT_ERR=%ERR% 
    echo *=========!!!!!!!!!!!!!!!!!!!!!!=========*
    echo ACTION FAILED!  ErrorLevel=%ERR%
    echo *=========!!!!!!!!!!!!!!!!!!!!!!=========*
    echo.
)";

        private const string ERRORHANDLING =
            @"                       
IF %SCRIPT_ERR% NEQ 0 GOTO END_FAILURE  

:END_SUCCESS
echo.
echo *========================================*
echo %0 completed successfully.
echo *========================================*
echo.
exit /B 0

:END_FAILURE
echo.
echo *=========!!!!!!!!!!!!!!!!!!!!!!=========*
echo SCRIPT %0 FAILED!
echo *=========!!!!!!!!!!!!!!!!!!!!!!=========*
echo.
pause
exit /B 1

:END";


        private static readonly string HEADER = string.Format(
            @"@echo off
ECHO.
ECHO Script Author: Solution Power Tool (SolPowerTool).
ECHO {1}
ECHO Created: {0:f}
ECHO.
REM WARINING!  This file is auto-generated!
REM Do not modify this file manually as it will likely be overwritten.

SET SCRIPT_ERR=0

",
            DateTime.Now,
            Assembly.GetExecutingAssembly().FullName);

        #endregion

        static TeamFoundationClient()
        {
            TF_EXE = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Microsoft Visual Studio 10.0\Common7\IDE\TF.exe");
        }

        public static bool Checkout(string file)
        {
            return Checkout(new[] {file});
        }

        public static bool Checkout(IEnumerable<string> files)
        {
            if (!File.Exists(TF_EXE))
            {
                MessageBox.Show("File not found: " + TF_EXE);
                return false;
            }

            string cmdFile = Path.GetTempFileName();
            File.Delete(cmdFile);
            cmdFile = cmdFile + ".cmd";
            using (var sr = new StreamWriter(cmdFile))
            {
                sr.WriteLine(HEADER);
                int i = 0;
                int count = files.Count();
                foreach (string file in files)
                {
                    sr.WriteLine(string.Format("echo {0} of {1} : {2}", ++i, count, file));
                    sr.WriteLine(string.Format("\"{0}\" checkout \"{1}\"", TF_EXE, file));
                    sr.WriteLine(ERRORLEVELCHECK);
                }
                sr.WriteLine(ERRORHANDLING);
            }
            Process process = Process.Start(cmdFile);
            Debug.Assert(process != null);
            process.WaitForExit();
            File.Delete(cmdFile);
            return (process.ExitCode == 0);
        }
    }
}