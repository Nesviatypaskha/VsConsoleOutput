using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using System.IO;
using Process = System.Diagnostics.Process;

namespace VsConsoleOutput
{
    internal sealed class DebugManager : IVsDebuggerEvents, IDebugEventCallback2
    {
        private DTE2 _dte2;
        private readonly IVsDebugger _debugger;
        private readonly IVsDebugger2 _debugger2;
        private IVsSolutionBuildManager _solutionBuildManager;
        private DebugProcessVS _debugProcess;
        private uint _cookie;
        private DBGMODE _debug_mode;
        private bool isAttached;

        private EnvDTE80.Commands2 commands;

        private static DebugManager _instance;
        private static readonly object _padlock = new object();

        public DebugManager()
        {
            _dte2 = VsConsoleOutputPackage.getDTE2();
            _debugger = VsConsoleOutputPackage.getDebugger();
            //_solutionBuildManager = VsConsoleOutputPackage.getSolutionBuildManager();
            commands = _dte2.Commands as EnvDTE80.Commands2;
            isAttached = false;
        }


        public static void Instantiate()
        {
            lock (_padlock)
            {
                if (_instance != null)
                    throw new InvalidOperationException(string.Format("{0} of Resurrect is already instantiated.", _instance.GetType().Name));
                _instance = new DebugManager();
            }
        }

        public static DebugManager Instance
        {
            get { return _instance; }
        }

        public void Advise()
        {
            if (_instance == null)
            {
                Instantiate();
            }
            _debugger.AdviseDebuggerEvents(this, out _cookie);
            _debugger.AdviseDebugEventCallback(this);
        }

        public void Unadvise()
        {
            _debugger.UnadviseDebuggerEvents(_cookie);
            _debugger.UnadviseDebugEventCallback(this);
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
                    break;
                default:
                    Output.Log("DBGMODE --- UNKNOWN");
                    break;
            };
            _debug_mode = mode;
            return VSConstants.S_OK;
        }

        private void GetFullInfo()
        {
            Output.Log("--- GetFullInfo START");
            DTE2 dte2 = VsConsoleOutputPackage.getDTE2();
            Debugger2 debugger2 = dte2.Debugger as Debugger2;
            SolutionBuild2 solutionBuild2 = dte2.Solution.SolutionBuild as SolutionBuild2;
            SolutionConfiguration2 solutionConfiguration2 = (SolutionConfiguration2)solutionBuild2.ActiveConfiguration;
            // RestartDebug only in debug mode!!!
            if (solutionConfiguration2.Name == "Debug")
            {
                Output.Log("--- SolutionConfiguration: Debug");
                foreach (String s in (Array)solutionBuild2.StartupProjects)
                {
                    Output.Log("---     StartupProject :", s);
                    foreach (Process2 process in debugger2.DebuggedProcesses)
                    {
                        string fileName = Path.GetFileName(process.Name);
                        Output.Log("---- DebuggedProcesses : {0}", fileName);
                        Output.Log("---- IsBeingDebugged   : {0}", process.IsBeingDebugged);
                        Output.Log("---- UserName          : {0}", process.UserName);

                        System.Diagnostics.Process p = System.Diagnostics.Process.GetProcessById(process.ProcessID);

                        Output.Log("---* ProcessName       : {0}", p.ProcessName);
                        //Output.Log("---* ProcessName       : {0}", p.OutputDataReceived);
                        //Output.Log("---* StandardOutput    : {0}", p.StandardOutput);

                        //fro all debuged processes in solution get
                        string process_NAME;
                        string process_FILENAME;
                        string process_BASENAME;
                        string process_MONIKERNAME;
                        string process_URL;
                        string process_TITLE;
                        string process_STARTPAGEURL;
                        //p.GetName(enum_GETNAME_TYPE.GN_NAME, out process_NAME);
                        //p.GetName(enum_GETNAME_TYPE.GN_FILENAME, out process_FILENAME);
                        //p.GetName(enum_GETNAME_TYPE.GN_BASENAME, out process_BASENAME);
                        //p.GetName(enum_GETNAME_TYPE.GN_MONIKERNAME, out process_MONIKERNAME);
                        //p.GetName(enum_GETNAME_TYPE.GN_URL, out process_URL);
                        //p.GetName(enum_GETNAME_TYPE.GN_TITLE, out process_TITLE);
                        //p.GetName(enum_GETNAME_TYPE.GN_STARTPAGEURL, out process_STARTPAGEURL);

                        //Output.Log("---         GN_NAME: {0}", process_NAME);
                        //Output.Log("---     GN_FILENAME: {0}", process_FILENAME);
                        //Output.Log("---     GN_BASENAME: {0}", process_BASENAME);
                        //Output.Log("---  GN_MONIKERNAME: {0}", process_MONIKERNAME);
                        //Output.Log("---          GN_URL: {0}", process_URL);
                        //Output.Log("---        GN_TITLE: {0}", process_TITLE);
                        //Output.Log("--- GN_STARTPAGEURL: {0}", process_STARTPAGEURL);
                    }
                }

               

                //TODO: PROCESS_INFO

            }
            Output.Log("--- GetFullInfo END -----------------------");
        }

        private void StandardOutputReceiver(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrEmpty(outLine.Data))
                return;
            Output.Log("Output>{0}", outLine.Data);
            Output.Console("Output>{0}", outLine.Data);
        }
        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program,
                            IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {

            ///// 
            ///IDebugEngine2::DestroyProgram


            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/76e2621f-270c-4de1-bd87-e6fd98244ac4/ad7-custom-debugger-trace-logs?forum=vsx // AD7 custom debugger trace logs
            // IDebugPortEx2::LaunchSuspended
            // test and monitor https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivsdebugger4?view=visualstudiosdk-2017 
            // EnumCurrentlyDebuggingProjects(IEnumHierarchies)	Returns the set of projects that have been launched through a debugger launch(F5) and that the debugger is currently debugging.

            if ((debugEvent is IDebugSessionCreateEvent2) || (riidEvent.ToString("B") == "2c2b15b7-fc6d-45b3-9622-29665d964a76"))
                Output.Log("debugEvent is IDebugSessionCreateEvent2"); //"IDebugSessionCreateEvent2","2c2b15b7-fc6d-45b3-9622-29665d964a76"
            else if ((debugEvent is IDebugProcessCreateEvent2) || (riidEvent.ToString("B") == "bac3780f-04da-4726-901c-ba6a4633e1ca"))
                Output.Log("debugEvent is IDebugProcessCreateEvent2"); //"IDebugProcessCreateEvent2","bac3780f-04da-4726-901c-ba6a4633e1ca"
            else if ((debugEvent is IDebugCustomEvent110) || (riidEvent.ToString("B") == "2615d9bc-1948-4d21-81ee-7a963f20cf59"))
                Output.Log("debugEvent is IDebugCustomEvent110 "); //"IDebugProcessCreateEvent2","2615d9bc-1948-4d21-81ee-7a963f20cf59"
            else if ((debugEvent is IDebugProgramCreateEvent2) || (riidEvent.ToString("B") == "96cd11ee-ecd4-4e89-957e-b5d496fc4139"))
                Output.Log("debugEvent is IDebugProgramCreateEvent2 "); //"IDebugProgramCreateEvent2","96cd11ee-ecd4-4e89-957e-b5d496fc4139"
            else if ((debugEvent is IDebugModuleLoadEvent2) || (riidEvent.ToString("B") == "989db083-0d7c-40d1-a9d9-921bf611a4b2"))
                Output.Log("debugEvent is IDebugModuleLoadEvent2 "); //"IDebugModuleLoadEvent2","989db083-0d7c-40d1-a9d9-921bf611a4b2"
            else if ((debugEvent is IDebugThreadCreateEvent2) || (riidEvent.ToString("B") == "2090ccfc-70c5-491d-a5e8-bad2dd9ee3ea"))
                //  This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread is created in a program being debugged.
                Output.Log("debugEvent is IDebugThreadCreateEvent2 "); //"IDebugThreadCreateEvent2","2090ccfc-70c5-491d-a5e8-bad2dd9ee3ea"
            else if ((debugEvent is IDebugTelemetryDetailsEvent150) || (riidEvent.ToString("B") == "8f2652b2-cd3c-4aed-a946-a3db6f379412"))
                Output.Log("debugEvent is IDebugTelemetryDetailsEvent150"); //"IDebugTelemetryDetailsEvent150","8f2652b2-cd3c-4aed-a946-a3db6f379412"
            else if ((debugEvent is IDebugLoadCompleteEvent2) || (riidEvent.ToString("B") == "b1844850-1349-45d4-9f12-495212f5eb0b"))
                Output.Log("debugEvent is IDebugLoadCompleteEvent2"); //"IDebugLoadCompleteEvent2","b1844850-1349-45d4-9f12-495212f5eb0b"
            else if ((debugEvent is IDebugEntryPointEvent2) || (riidEvent.ToString("B") == "e8414a3e-1642-48ec-829e-5f4040e16da9"))
                Output.Log("debugEvent is IDebugEntryPointEvent2"); //"IDebugEntryPointEvent2","e8414a3e-1642-48ec-829e-5f4040e16da9"
            else if ((debugEvent is IDebugProcessContinueEvent100) || (riidEvent.ToString("B") == "c703ebea-42e7-4768-85a9-692eecba567b"))
                Output.Log("debugEvent is IDebugProcessContinueEvent100 "); //"IDebugProcessContinueEvent100 ","c703ebea-42e7-4768-85a9-692eecba567b"
            else if ((debugEvent is IDebugThreadDestroyEvent2) || (riidEvent.ToString("B") == "2c3b7532-a36f-4a6e-9072-49be649b8541"))
                Output.Log("debugEvent is IDebugThreadDestroyEvent2"); //"IDebugThreadDestroyEvent2","2c3b7532-a36f-4a6e-9072-49be649b8541"
            else if ((debugEvent is IDebugProgramDestroyEvent2) || (riidEvent.ToString("B") == "e147e9e3-6440-4073-a7b7-a65592c714b5"))
                Output.Log("debugEvent is IDebugProgramDestroyEvent2"); //"IDebugProgramDestroyEvent2","e147e9e3-6440-4073-a7b7-a65592c714b5"
            else if ((debugEvent is IDebugProcessDestroyEvent2) || (riidEvent.ToString("B") == "3e2a0832-17e1-4886-8c0e-204da242995f"))
                Output.Log("debugEvent is IDebugProcessDestroyEvent2"); //"IDebugProcessDestroyEvent2","3e2a0832-17e1-4886-8c0e-204da242995f"
            else if ((debugEvent is IDebugSessionDestroyEvent2) || (riidEvent.ToString("B") == "f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"))
            {
                isAttached = false;
                Output.Log("debugEvent is IDebugSessionDestroyEvent2"); //"IDebugSessionDestroyEvent2","f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"
            }
                
            else
                Output.Log("Event Command.name = {0}.{1}", riidEvent.ToString("B"), attributes);

            // https://docs.microsoft.com/ru-ru/visualstudio/extensibility/debugger/reference/idebugprogram2-getencupdate?view=vs-2015&redirectedfrom=MSDN


            if (process == null)
                return VSConstants.S_OK;
            SolutionBuild2 solutionBuild2 = _dte2.Solution.SolutionBuild as SolutionBuild2;
            SolutionConfiguration2 solutionConfiguration2 = (SolutionConfiguration2)solutionBuild2.ActiveConfiguration;
            
            if ((!isAttached) && (solutionConfiguration2.Name == "Debug"))
            {
                isAttached = true;
                string process_NAME;
                string process_FILENAME;
                string process_BASENAME;
                string process_MONIKERNAME;
                string process_URL;
                string process_TITLE;
                string process_STARTPAGEURL;
                
                process.GetName(enum_GETNAME_TYPE.GN_NAME, out process_NAME);
                process.GetName(enum_GETNAME_TYPE.GN_FILENAME, out process_FILENAME);
                process.GetName(enum_GETNAME_TYPE.GN_BASENAME, out process_BASENAME);
                process.GetName(enum_GETNAME_TYPE.GN_MONIKERNAME, out process_MONIKERNAME);
                process.GetName(enum_GETNAME_TYPE.GN_URL, out process_URL);
                process.GetName(enum_GETNAME_TYPE.GN_TITLE, out process_TITLE);
                process.GetName(enum_GETNAME_TYPE.GN_STARTPAGEURL, out process_STARTPAGEURL);

                Output.Log("GN_NAME: {0}", process_NAME);
                Output.Log("GN_FILENAME: {0}", process_FILENAME);
                Output.Log("GN_BASENAME: {0}", process_BASENAME);
                Output.Log("GN_MONIKERNAME: {0}", process_MONIKERNAME);
                Output.Log("GN_URL: {0}", process_URL);
                Output.Log("GN_TITLE: {0}", process_TITLE);
                Output.Log("GN_STARTPAGEURL: {0}", process_STARTPAGEURL);

                //TODO: PROCESS_INFO
                var info = new PROCESS_INFO[1];
                process.GetInfo(enum_PROCESS_INFO_FIELDS.PIF_ALL, info);

                

                //string commandLine = GetCommandLine((System.Diagnostics.Process)process);
                //process.Detach();
                //process.Terminate();
                if (_debugProcess == null)
                {
                    _debugProcess = new DebugProcessVS();

                }
                _debugProcess.StartProcess(process_FILENAME, "", "");
                //foreach (EnvDTE.Process process in processes)
                //{
                //    process.Terminate(true);
                //    foreach (EnvDTE.Program program in process.Programs)
                //    {
                //        program.Process.Terminate(true);
                //    }
                //}



                //DebugSetProcessKillOnExit(true);

            }
            return VSConstants.S_OK;
        }

        private static string GetCommandLine(Process process)
        {
            string cmdLine = null;
            using (var searcher = new System.Management.ManagementObjectSearcher(
              $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
            {
                // By definition, the query returns at most 1 match, because the process 
                // is looked up by ID (which is unique by definition).
                using (var matchEnum = searcher.Get().GetEnumerator())
                {
                    if (matchEnum.MoveNext()) // Move to the 1st item.
                    {
                        cmdLine = matchEnum.Current["CommandLine"]?.ToString();
                    }
                }
            }
            if (cmdLine == null)
            {
                // Not having found a command line implies 1 of 2 exceptions, which the
                // WMI query masked:
                // An "Access denied" exception due to lack of privileges.
                // A "Cannot process request because the process (<pid>) has exited."
                // exception due to the process having terminated.
                // We provoke the same exception again simply by accessing process.MainModule.
                var dummy = process.MainModule; // Provoke exception.
            }
            return cmdLine;
        }

        private void RestartDebug()
        {
            // TODO check IVsDebugger2.GetConsoleHandlesForProcess(UInt32, UInt64, UInt64, UInt64) Method
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/debugger/reference/idebugenginelaunch2-launchsuspended?view=vs-2019
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/debugger/launching-a-program?view=vs-2019
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/debugger/visual-studio-debugger-extensibility?view=vs-2019


            // In Method IVsDebuggableProjectCfg2.OnBeforeDebugLaunch(UInt32) need to be implement rediretion of input an output. Need to update in
            // IVsDebugger2.LaunchDebugTargets2(UInt32, IntPtr) [in, out] Array of !!!VsDebugTargetInfo2!!! structures describing the programs to launch or attach to.
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivsdebugger2.launchdebugtargets2?view=visualstudiosdk-2017
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.ivsdebuggableprojectcfg2.onbeforedebuglaunch?view=visualstudiosdk-2017#Microsoft_VisualStudio_Shell_Interop_IVsDebuggableProjectCfg2_OnBeforeDebugLaunch_System_UInt32_


            // for reading https://stackoverflow.com/questions/31813996/attaching-to-process-in-visual-studio-package
            // 

            // TODO: https://stackoverflow.com/questions/15524038/visual-studio-how-to-get-idebugengine2-from-vs-package-except-ivsloader
            // TODO: https://docs.microsoft.com/en-us/dotnet/api/envdte80?view=visualstudiosdk-2017 - check debugg events

            // https://stackoverflow.com/questions/15582736/how-to-set-break-on-all-exceptions-from-a-package

            //        process.OutputDataReceived += new System.Diagnostics.
            //          DataReceivedEventHandler(StandardOutputReceiver);
            //        process.ErrorDataReceived += new System.Diagnostics.
            //            DataReceivedEventHandler(StandardErrorReceiver);


            Debugger2 _dbg = _dte2.Debugger as Debugger2;
            //TODO: redirect ONLY if Console present
            //1. Find StartupProjects
            SolutionBuild2 solutionBuild2 = _dte2.Solution.SolutionBuild as SolutionBuild2;
            SolutionConfiguration2 solutionConfiguration2 = (SolutionConfiguration2)solutionBuild2.ActiveConfiguration;

            // ResturtDebug only in debug mode!!!
            if (solutionConfiguration2.Name == "Debug")
            {

            }

            // 30.10.2019 03:18:00: OnBeforeExecute Debug.Start
            // 30.10.2019 03:18:01: OnAfterExecute Debug.Start
            // 30.10.2019 03:18:03: DBGMODE_Run
            // 30.10.2019 03:18:03: ------------StartupProjects--------------
            // 30.10.2019 03:18:03: test.vcxproj
            // 30.10.2019 03:18:03:                                            
            // 30.10.2019 03:18:03: DebuggedProcesses: test.exe
            // 30.10.2019 03:18:03: Return relative path to startup projects: 
            //    test.vcxproj
            // DONE     // SolutionConfiguration: Debug
            // A build has occurred.
            // 30.10.2019 03:18:04: GN_NAME: test.exe
            // 30.10.2019 03:18:04: GN_FILENAME: C: \Users\Alex\source\repos\test\Debug\test.exe
            // 30.10.2019 03:18:04: GN_BASENAME: test.exe
            // 30.10.2019 03:18:04: GN_MONIKERNAME:
            // 30.10.2019 03:18:04: GN_URL: file://C:\Users\Alex\source\repos\test\Debug\test.exe
            // 30.10.2019 03:18:04: GN_TITLE:
            // 30.10.2019 03:18:04: GN_STARTPAGEURL:
            // 30.10.2019 03:18:04: SessionName:
            // 30.10.2019 03:18:04: debugEvent is IDebugProcessCreateEvent2
            // 30.10.2019 03:18:04: debugEvent is IDebugLoadCompleteEvent2



            foreach (String s in (Array)solutionBuild2.StartupProjects)
            {
                Output.Log("------------ StartupProjects --------------");
                Output.Log(s);
                Output.Log("                                           ");
                //msg += "\n   " + s;
            }



            //Processes : IEnumerable
            foreach (Process2 process in _dbg.DebuggedProcesses)
            {
                //_dbg.Stop();
                string fileName = Path.GetFileName(process.Name);
                Output.Log("DebuggedProcesses : {0}", fileName);
                //TODO: int CanTerminateProcess (IDebugProcess2 pProcess);
                //TODO: ProcessStartInfo.RedirectStandardOutput Property
                //TODO:  process.IsBeingDebugged() ?

                //process.Terminate(); TODO: GetPhysicalProcessId();
                //_dbg.Go(false);
            }
            SolutionBuild2 sb = _dte2.Solution.SolutionBuild as SolutionBuild2;
            SolutionConfiguration2 sc = (SolutionConfiguration2)sb.ActiveConfiguration;
            vsBuildState vsBS;
            string msg = "Return relative path to startup projects: ";
            foreach (String s in (Array)sb.StartupProjects)
            {
                msg += "\n   " + s;
            }
            msg += "\nSolutionConfiguration: " + sc.Name;
            vsBS = sb.BuildState;
            if (vsBS == vsBuildState.vsBuildStateDone)
                msg += "\nA build has occurred.";
            else if (vsBS == vsBuildState.vsBuildStateInProgress)
                msg += "\nA build is in progress.";
            else msg += "\nA build has not occurred.";
            Output.Log(msg);

        }
    }
}


