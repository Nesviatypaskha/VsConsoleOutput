﻿using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsConsoleOutput
{
    public static class Output
    {
#if DEBUG
        private static OutputWindowPane _loggerPane;
#endif
        private static IVsOutputWindowPane _consolePane;

        public static void Initialize()
        {
            //ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                DTE2 dte = VsConsoleOutputPackage.getDTE2();
                if ((dte != null) && (VsConsoleOutputPackage.getDTE2() != null))
                {
#if DEBUG
                    if (_loggerPane == null)
                    {
                        _loggerPane = dte.ToolWindows.OutputWindow.OutputWindowPanes.Add("VSOutputLogger");
                        _loggerPane.Activate();
                        _loggerPane.Clear();
                        //_loggerPane.Guid = "204E2A26 - 7BD7 - 4632 - 8043 - 18D94C179103";
                    }
#endif
                    if (_consolePane == null)
                    {
                        IVsOutputWindow output = VsConsoleOutputPackage.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                        Guid customGuid = new Guid("204E2A26-7BD7-4632-8043-18D94C179103");
                        string customTitle = "Console";
                        output.CreatePane(ref customGuid, customTitle, 1, 1);
                        //IVsOutputWindowPane customPane;
                        output.GetPane(ref customGuid, out _consolePane);
                        _consolePane.Activate();
                    }
                }
            }
            catch (Exception ex)
            {
                ex = ex;
                // TODO: Add catch
            }
        }

        public static void ActivateConsole()
        {
            _consolePane.Activate();
        }
        public static void ClearConsole()
        {
            _consolePane.Clear();
        }
#if DEBUG
        public static void ActivateLog()
        {
            _loggerPane.Activate();
        }
        public static void ClearLog()
        {
            _loggerPane.Clear();
        }
#endif
        public static void Log(string message)
        {
#if DEBUG
            try
            {
                //if (_loggerPane == null)
                //    Output.Initialize();
                if (string.IsNullOrEmpty(message))
                    return;
                OutputStringLog(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
            }
            catch
            {
                // TODO: Add catch
            }
#endif
        }

        public static void Log(string format, params object[] args)
        {
#if DEBUG
            try
            { 
                //if (_loggerPane == null)
                //    Output.Initialize();
                if (string.IsNullOrEmpty(format))
                    return;
                OutputStringLog(DateTime.Now.ToString() + ": " + string.Format(format, args) + Environment.NewLine);
            }
            catch
            {
                // TODO: Add catch
            }
#endif
        }
#if DEBUG
        private static void OutputStringLog(string text)
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
#endif
        public static void Console(string message)
        {
            try
            {
                //if (_consolePane == null)
                //    Output.Initialize();
                if (string.IsNullOrEmpty(message))
                    return;
                OutputStringConsole(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
            }
            catch
            {
                // TODO: Add catch
            }
        }

        public static void Console(string format, params object[] args)
        {
            try
            {
                //if (_consolePane == null)
                //    Output.Initialize();
                if (string.IsNullOrEmpty(format))
                    return;
                OutputStringConsole(DateTime.Now.ToString() + ": " + string.Format(format, args) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // TODO: Add catch
                ex = ex;
            }
        }

        private static void OutputStringConsole(string text)
        {
            // TODO add timer System.Timer
            //ThreadHelper.ThrowIfNotOnUIThread("VSoutput.Logger.OutputStringConsole");
            if (_consolePane != null)
            {
                try
                {
                    _consolePane.Activate();
                    _consolePane.OutputString(text);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    //TODO: log Exception
                }
            }
        }
    }
}
