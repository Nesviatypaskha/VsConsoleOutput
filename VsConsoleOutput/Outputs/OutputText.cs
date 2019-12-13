using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSConsoleOutputBeta
{
    public static class OutputText
    {
        public static void WriteInNewPane(DTE2 dte2, string name, string message, Guid guid = new Guid())
        {   
            if (dte2 != null)
            {
                IVsOutputWindow output = VSConsoleOutputBetaPackage.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                Guid customGuid = new Guid("204E2A26-7BD7-4632-8043-18D94C179103");
                string customTitle = "Console";
                output.CreatePane(ref customGuid, customTitle, 1, 1);
                IVsOutputWindowPane outputPane;
                output.GetPane(ref customGuid, out outputPane);
                outputPane.OutputString(message + "\n");
                outputPane.Activate();
            }
        }

        public static void Write(string name, string message)
        {
#if !DEBUG
            if (name == "VSOutputDebugLog")
            {
                return;
            }
#endif
            try
            {
                DTE2 dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;
                if (dte2 != null)
                {
                    OutputWindowPanes panes = dte2.ToolWindows.OutputWindow.OutputWindowPanes;
                    foreach (OutputWindowPane pane in panes)
                    {
                        if (pane.Name == name)
                        {
                            pane.OutputString(message + "\n");
                            pane.Activate();
                            return;
                        }
                    }
                    if (name == "Console")
                    {
                        WriteInNewPane(dte2, name, message, new Guid("204E2A26-7BD7-4632-8043-18D94C179103"));
                    }
                    else
                    {
                        WriteInNewPane(dte2, name, message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }



#if DEBUG
        private static OutputWindowPane _loggerPane;
#endif
        private static IVsOutputWindowPane _consolePane;



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
                if (string.IsNullOrEmpty(message))
                    return;
                OutputStringLog(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
#endif
        }

        public static void Log(string format, params object[] args)
        {
#if DEBUG
            try
            { 

                if (string.IsNullOrEmpty(format))
                    return;
                OutputStringLog(DateTime.Now.ToString() + ": " + string.Format(format, args) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
#endif
        }
#if DEBUG
        private static void OutputStringLog(string text)
        {
            if (_loggerPane != null)
            {
                try
                {
                    _loggerPane.Activate();
                    _loggerPane.OutputString(text);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }
#endif
        public static void Console(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                    return;
                OutputStringConsole( message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public static void Console(string format, params object[] args)
        {
            try
            {
                if (string.IsNullOrEmpty(format))
                    return;
                OutputStringConsole(string.Format(format, args) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private static void OutputStringConsole(string text)
        {
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
                }
            }
        }
    }
}
