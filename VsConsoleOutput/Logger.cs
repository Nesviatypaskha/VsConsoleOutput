using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsConsoleOutput
{
    public static class Logger
    {
        private static OutputWindowPane _loggerPane;

        public static void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                DTE2 dte = VsConsoleOutputPackage.getDTE2();
                if ((dte != null) && (VsConsoleOutputPackage.getDTE2() != null))
                {
                    _loggerPane = dte.ToolWindows.OutputWindow.OutputWindowPanes.Add("VSOutputLogger");
                    _loggerPane.Activate();
                    _loggerPane.Clear();
                }
            }
            catch
            {
                // TODO: Add catch
            }
        }

        public static void Log(string message)
        {
            if (_loggerPane == null)
                Logger.Initialize();
            if (string.IsNullOrEmpty(message))
                return;
            OutputString(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
        }

        public static void Log(string format, params object[] args)
        {
            if (_loggerPane == null)
                Logger.Initialize();
            if (string.IsNullOrEmpty(format))
                return;
            OutputString(DateTime.Now.ToString() + ": " + string.Format(format, args) + Environment.NewLine);
        }

        private static void OutputString(string text)
        {
            // TODO add timer System.Timer
            ThreadHelper.ThrowIfNotOnUIThread("VSoutput.Logger.OutputString");
            if (_loggerPane != null)
            {
                try
                {
                    _loggerPane.Activate();
                    _loggerPane.OutputString(text);
                }
                catch (Exception)
                {
                    //TODO: log Exception
                }
            }
        }
    }
}
