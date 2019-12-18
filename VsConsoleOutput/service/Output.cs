using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace service
{
    public static class Output
    {
        public static void Write(DTE2 dte2, string name, string message, Guid guid)
        {
            if (dte2 != null)
            {
                var a_Context1 = package.VSConsoleOutputPackage.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                var a_Context2 = (IVsOutputWindowPane)null;
                {
                    a_Context1.CreatePane(ref guid, name, 1, 1);
                }
                {
                    a_Context1.GetPane(ref guid, out a_Context2);
                }
                if (a_Context2 != null)
                {
                    a_Context2.OutputString(message + "\n");
                    a_Context2.Activate();
                }
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
                var dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;
                if (dte2 != null)
                {
                    var panes = dte2.ToolWindows.OutputWindow.OutputWindowPanes;
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
                        Write(dte2, name, message, new Guid("204E2A26-7BD7-4632-8043-18D94C179103"));
                    }
                    else
                    {
                        Write(dte2, name, message, new Guid());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}
