﻿using Microsoft.VisualStudio.Shell.Interop;
using Shell = Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using System;

namespace service
{
    public static class Output
    {
        private static IVsOutputWindowPane s_Pane = null;

        public static async Task ClearAsync()
        {
            await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                var a_Context = __GetPane();
                if (a_Context != null)
                {
                    a_Context.Clear();
                    a_Context.Activate();
                }
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
        }

        public static async Task WriteLineAsync(string message)
        {
            try
            {
                await Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var a_Context = __GetPane();
                if (a_Context != null)
                {
                    a_Context.OutputStringThreadSafe(message);
                    a_Context.Activate();
                }
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
        }

        public static void WriteError(string message)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ERROR: " + message + " // <CONSOLE>\n");
#endif
        }

        private static IVsOutputWindowPane __GetPane()
        {
            Shell.ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (s_Pane == null)
                {
                    var a_Context = package.VSConsoleOutputPackage.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    {
                        var a_Context1 = new Guid("204E2A26-7BD7-4632-8043-18D94C179103");
                        {
                            a_Context.CreatePane(ref a_Context1, "Console", 1, 1);
                            a_Context.GetPane(ref a_Context1, out s_Pane);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
            return s_Pane;
        }
    }
}
