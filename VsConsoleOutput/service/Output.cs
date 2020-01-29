
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;

namespace service
{
    public static class Output
    {
        public const string DEBUG = "VSOutputDebugLog";
        public const string CONSOLE = "Console";
        private static Dictionary<string, IVsOutputWindowPane> m_Outputs;
        public static IVsOutputWindowPane GetPane(string name)
        {
#if !DEBUG
            if (name == DEBUG)
            {
                return null;
            }
#endif
            var result = (IVsOutputWindowPane)null;
            if (m_Outputs == null)
            {
                m_Outputs = new Dictionary<string, IVsOutputWindowPane>();
            }
            bool isPresent = m_Outputs.TryGetValue(name, out result);
            if (!isPresent)
            {
                result = CreatePane(name);
            }
            return result;
        }
        private static IVsOutputWindowPane CreatePane(string name)
        {
            var result = (IVsOutputWindowPane)null;
            var dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            var a_Context1 = package.VSConsoleOutputPackage.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var guid = new Guid();
            if (name == CONSOLE)
            {
                guid = new Guid("204E2A26-7BD7-4632-8043-18D94C179103");
            }
            a_Context1.CreatePane(ref guid, name, 1, 1);
            a_Context1.GetPane(ref guid, out result);
            m_Outputs.Add(name, result);

            result.Activate();
            return result;
        }

        public static void ActivatePane(string name)
        {
            try
            {
                var a_Context1 = GetPane(name);
                if (a_Context1 != null)
                    a_Context1.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        public static void Write(string name, string message)
        {
            try
            {
                var a_Context1 = GetPane(name);
                if (a_Context1 != null)
                {
                    a_Context1.Activate();
                    a_Context1.OutputString(message + "\n");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}

