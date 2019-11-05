using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsConsoleOutput
{
    class DebuggerEventsMonitor : IVsDebuggerEvents
    {
        private static DebuggerEventsMonitor _instance;
        private readonly IVsDebugger _debugger;
        private uint _cookie;
        private static readonly object _padlock = new object();
        public DebuggerEventsMonitor()
        {
            _debugger = VsConsoleOutputPackage.getDebugger();
        }

        public static void Instantiate()
        {
            lock (_padlock)
            {
                if (_instance != null)
                    throw new InvalidOperationException(string.Format("{0} of Resurrect is already instantiated.", _instance.GetType().Name));
                _instance = new DebuggerEventsMonitor();
            }
        }
        public static DebuggerEventsMonitor Instance
        {
            get 
            {
                if (_instance == null) Instantiate();
                return _instance; 
            }
        }

        public void Advise()
        {
            if (_instance == null)
            {
                Instantiate();
                _debugger.AdviseDebuggerEvents(this, out _cookie);
                _debugger.AdviseDebugEventCallback(this);
            }
        }

        public void Unadvise()
        {
            if (_instance == null)
            {
                _debugger.UnadviseDebuggerEvents(_cookie);
                _debugger.UnadviseDebugEventCallback(this);
            }
        }

        public int OnModeChange(DBGMODE mode)
        {
            switch (mode)
            {
                case DBGMODE.DBGMODE_Break:
                    Output.Log("DBGMODE_Break");
                    break;
                case DBGMODE.DBGMODE_Design:
                    Output.Log("DBGMODE_Design");
                    break;
                case DBGMODE.DBGMODE_Enc:
                    Output.Log("DBGMODE_Enc");
                    break;
                case DBGMODE.DBGMODE_EncMask:
                    Output.Log("DBGMODE_EncMask");
                    break;
                case DBGMODE.DBGMODE_Run:
                    Output.Log("DBGMODE_Run");
                    GetFullInfo();
                    break;
                default:
                    Output.Log("DBGMODE --- UNKNOWN");
                    break;
            };
            return VSConstants.S_OK;
        }

        private void GetFullInfo()
        {
            DTE2 dte2 = VsConsoleOutputPackage.getDTE2();
            Debugger2 debugger2 = dte2.Debugger as Debugger2;
            SolutionBuild2 solutionBuild2 = dte2.Solution.SolutionBuild as SolutionBuild2;
            SolutionConfiguration2 solutionConfiguration2 = (SolutionConfiguration2)solutionBuild2.ActiveConfiguration;
            // ResturtDebug only in debug mode!!!
            if (solutionConfiguration2.Name == "Debug")
            {
                Output.Log("------ SolutionConfiguration: Debug ------");
                foreach (String s in (Array)solutionBuild2.StartupProjects)
                {
                    Output.Log("------------ StartupProjects --------------");
                    Output.Log(s);
                    foreach (Process2 process in debugger2.DebuggedProcesses)
                    {
                        string fileName = Path.GetFileName(process.Name);
                        Output.Log("DebuggedProcesses : {0}", fileName);
                        Output.Log("IsBeingDebugged : {0}", process.IsBeingDebugged);
                    }
                }

                //  fro all debuged processes in solution get 

            }
        }
    }
}
