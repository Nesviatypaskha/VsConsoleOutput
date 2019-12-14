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
        public static void WriteInNewPane(DTE2 dte2, string name, string message, Guid guid)
        {
            if (dte2 != null)
            {
                IVsOutputWindow output = VSConsoleOutputBetaPackage.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                output.CreatePane(ref guid, name, 1, 1);
                IVsOutputWindowPane outputPane;
                output.GetPane(ref guid, out outputPane);
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
                        WriteInNewPane(dte2, name, message, new Guid());
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
