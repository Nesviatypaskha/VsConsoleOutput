using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace service
{
    internal sealed class Debug : IDebugEventCallback2, IVsDebuggerEvents
    {
        public const string BREAKPOINT_MESSAGE = "VSOutputConsole connected";

        public bool m_attached;
        private bool m_added;
        private bool m_redirected;
        private bool m_ready;
        private bool m_entry;
        private bool m_was_stoped;
        private string m_language;
        private static uint _cookie;
        private System.Threading.Thread m_serverThread;
        private DTE m_DTE;
        private static IVsDebugger s_debugger;
        private static Debug s_Instance;
        private static readonly object s_Padlock = new object();

        public int OnModeChange(DBGMODE mode)
        {
            switch (mode)
            {
                case DBGMODE.DBGMODE_Break:
                    Output.Write(Output.CONSOLE,"-----------------------------------DBGMODE_Break");
                    break;
                case DBGMODE.DBGMODE_Design:
                    Output.Write(Output.CONSOLE, "-----------------------------------DBGMODE_Design");
                    break;
                case DBGMODE.DBGMODE_Enc:
                    Output.Write(Output.CONSOLE, "-----------------------------------DBGMODE_Enc");
                    break;
                case DBGMODE.DBGMODE_EncMask:
                    Output.Write(Output.CONSOLE, "-----------------------------------DBGMODE_EncMask");
                    break;
                case DBGMODE.DBGMODE_Run:
                    {
                        m_redirected = false;
                        clear();
                        if ((m_serverThread == null) || !m_serverThread.IsAlive)
                        {
                            m_serverThread = new System.Threading.Thread(output.Pipe.StartServer);
                            m_serverThread.Start();
                        }
                        Output.Write(Output.CONSOLE, "-----------------------------------DBGMODE_Run");
                        break;
                    }
                default:
                    Output.Write(Output.CONSOLE, "-----------------------------------DBGMODE --- UNKNOWN");
                    break;
            };
            return VSConstants.S_OK;
        }

        //public DebugManager()
        //{
        //    ThreadHelper.ThrowIfNotOnUIThread();
        //    _dte = VsConsoleOutputPackage.getDTE();
        //    _dte2 = VsConsoleOutputPackage.getDTE2();
        //    _debugger = VsConsoleOutputPackage.getDebugger();
        //    _debugger2 = VsConsoleOutputPackage.getDebugger2();
        //    //_solutionBuildManager = VsConsoleOutputPackage.getSolutionBuildManager();
        //    commands = _dte2.Commands as EnvDTE80.Commands2;
        //    isAttached = false;

        //    //DebuggerEvents debuggerEvents = _dte.Events.DebuggerEvents;

        //    //debuggerEvents.OnEnterBreakMode += OnEnterBreakModeHandler;
        //    //debuggerEvents.OnEnterRunMode += OnEnterRunModeHandler;
        //}

        ////public static void OnEnterBreakModeHandler(dbgEventReason reason, ref dbgExecutionAction execAction)
        ////{
        ////    Output.Log("OnEnterBreakModeHandler");
        ////}
        ////public static void OnEnterRunModeHandler(dbgEventReason reason)
        ////{
        ////    Output.Log("OnEnterRunModeHandler");
        //}

        public static void Initialize()
        {
            if (s_Instance == null)
            {
                Instantiate();
            }
            s_debugger.AdviseDebuggerEvents(s_Instance, out _cookie);
            s_debugger.AdviseDebugEventCallback(s_Instance);
        }

        public static void Finalize()
        {
            if (s_Instance != null)
            {
                s_debugger.UnadviseDebuggerEvents(_cookie);
                s_debugger.UnadviseDebugEventCallback(s_Instance);
            }
        }

        public Debug()
        {
            m_DTE = Package.GetGlobalService(typeof(SDTE)) as DTE;
            s_debugger = Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
            m_attached = false;
            m_added = false;
            m_ready = false;
            m_entry = false;
            m_was_stoped = false;
            m_redirected = false;
        }

        public static void Instantiate()
        {
            lock (s_Padlock)
            {
                if (s_Instance != null)
                    throw new InvalidOperationException(string.Format("{0} of Resurrect is already instantiated.", s_Instance.GetType().Name));
                s_Instance = new Debug();
            }
        }

        public static Debug Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    Instantiate();
                }
                return s_Instance;
            }
        }

        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program,
                            IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {
#if DEBUG
            if (debugEvent is IDebugSessionCreateEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugSessionCreateEvent2.{0}", attributes));
            else if (debugEvent is IDebugProcessCreateEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugProcessCreateEvent2.{0}", attributes));
            else if (debugEvent is IDebugCustomEvent110)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugCustomEvent110.{0}", attributes));
            else if (debugEvent is IDebugProgramCreateEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugProgramCreateEvent2.{0}", attributes));
            else if (debugEvent is IDebugModuleLoadEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugModuleLoadEvent2.{0}", attributes));
            else if (debugEvent is IDebugThreadCreateEvent2)
                //  This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread is created in a program being debugged.
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugThreadCreateEvent2.{0}", attributes));
            else if (debugEvent is IDebugTelemetryDetailsEvent150)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugTelemetryDetailsEvent150.{0}", attributes));
            else if (debugEvent is IDebugLoadCompleteEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugLoadCompleteEvent2.{0}", attributes));
            else if (debugEvent is IDebugEntryPointEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugEntryPointEvent2.{0}", attributes));
            else if (debugEvent is IDebugProcessContinueEvent100)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugProcessContinueEvent100.{0}", attributes));
            else if (debugEvent is IDebugThreadDestroyEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugThreadDestroyEvent2.{0}", attributes));
            else if (debugEvent is IDebugProgramDestroyEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugProgramDestroyEvent2.{0}", attributes));
            else if (debugEvent is IDebugProcessDestroyEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugProcessDestroyEvent2.{0}", attributes));
            else if (debugEvent is IDebugSessionDestroyEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugSessionDestroyEvent2.{0}", attributes));
            else if (debugEvent is IDebugBreakpointErrorEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugBreakpointErrorEvent2.{0}", attributes));
            else if (debugEvent is IDebugBreakpointBoundEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugBreakpointBoundEvent2.{0}", attributes));
            else if (debugEvent is IDebugMessageEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugMessageEvent2.{0}", attributes));
            else if (debugEvent is IDebugOutputStringEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugOutputStringEvent2.{0}", attributes));
            else if (debugEvent is IDebugExceptionEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugExceptionEvent2.{0}", attributes));
            else if (debugEvent is IDebugCurrentThreadChangedEvent100)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugCurrentThreadChangedEvent100.{0}", attributes));
            else if (debugEvent is IDebugBreakEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugBreakEvent2.{0}", attributes));
            else if (debugEvent is IDebugBreakpointEvent2)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugBreakpointEvent2.{0}", attributes));
            else if (debugEvent is IDebugThreadSuspendChangeEvent100)
                Output.Write(Output.CONSOLE, String.Format("debugEvent is IDebugThreadSuspendChangeEvent100.{0}", attributes));
            else
                Output.Write(Output.CONSOLE, String.Format("Event Command.name = {0}.{1}", riidEvent.ToString("B"), attributes));
            string threadstate = "UNKNOWN            ";

            var threadproperties = new THREADPROPERTIES[1];
            if (thread != null)
            {
                thread.GetThreadProperties(enum_THREADPROPERTY_FIELDS.TPF_ALLFIELDS, threadproperties);
                switch ((enum_THREADSTATE)threadproperties[0].dwThreadState)
                {
                    case enum_THREADSTATE.THREADSTATE_RUNNING:
                        threadstate = "THREADSTATE_RUNNING";
                        break;
                    case enum_THREADSTATE.THREADSTATE_STOPPED:
                        threadstate = "THREADSTATE_STOPPED";
                        break;
                    case enum_THREADSTATE.THREADSTATE_FRESH:
                        threadstate = "THREADSTATE_FRESH  ";
                        break;
                    case enum_THREADSTATE.THREADSTATE_DEAD:
                        threadstate = "THREADSTATE_DEAD   ";
                        break;
                    case enum_THREADSTATE.THREADSTATE_FROZEN:
                        threadstate = "THREADSTATE_FROZEN ";
                        break;
                    default:
                        break;
                }
            }
            Output.Write(Output.CONSOLE, String.Format("engine = {0}; process = {1}; program = {2}; thread = {3}, threadstate = {4}, m_added = {5}",
                    engine == null ? "NULL" : "YESS", process == null ? "NULL" : "YESS", program == null ? "NULL" : "YESS", thread == null ? "NULL" : "YESS", threadstate, m_added == true ? "TRUE" : "FALSE"));
#endif

            #region Last
            if (debugEvent is IDebugSessionDestroyEvent2)
            {
                clear();
            }
            else if (m_redirected)
            {
                Output.Write(Output.CONSOLE, " if (m_redirected)");
                return VSConstants.S_OK;
            }

            if (m_attached)
            {
                Output.Write(Output.CONSOLE, "if (m_attached)");
                RemoveBraekpoint();
                m_redirected = true;
            }

            if (debugEvent is IDebugEntryPointEvent2)
            {
                m_entry = true;
            }

            if (debugEvent is IDebugThreadCreateEvent2)
            {
                if (thread != null)
                {
                    thread.GetThreadProperties(enum_THREADPROPERTY_FIELDS.TPF_ALLFIELDS, threadproperties);
                    switch ((enum_THREADSTATE)threadproperties[0].dwThreadState)
                    {
                        case enum_THREADSTATE.THREADSTATE_STOPPED:
                            threadstate = "THREADSTATE_STOPPED";
                            m_was_stoped = true;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (thread != null)
            {
                if ((m_added && m_was_stoped && m_entry) || (debugEvent is IDebugMessageEvent2))
                {
                    RedirectStdStreams(thread);
                }


                if(m_entry)
                {
                    m_was_stoped = true;
                }

                if (!m_added)
                {
                    AddTracePoint(thread);
                }
            }

            Output.Write(Output.CONSOLE, " ----------------------------------------------------------------------------------------------------------END");
            return VSConstants.S_OK;
            #endregion
        }
        private void AddTracePoint(IDebugThread2 thread)
        {
            try
            {
                if (thread != null)
                {
                    IEnumDebugFrameInfo2 frame;
                    thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FUNCNAME | enum_FRAMEINFO_FLAGS.FIF_LANGUAGE | enum_FRAMEINFO_FLAGS.FIF_FRAME, 0, out frame);
                    uint frames;
                    frame.GetCount(out frames);
                    var frameInfo = new FRAMEINFO[1];
                    uint pceltFetched = 0;
                    while ((frame.Next(1, frameInfo, ref pceltFetched) == VSConstants.S_OK) && (pceltFetched > 0))
                    {
                        m_language = frameInfo[0].m_bstrLanguage;
                        if (m_language != "C#")
                        {
                            return;
                        }
                        var fr = frameInfo[0].m_pFrame as IDebugStackFrame2;
                        if (String.IsNullOrEmpty(frameInfo[0].m_bstrFuncName))
                        {
                            continue;
                        }
                        if (fr == null)
                        {
                            continue;
                        }
                        IDebugCodeContext2 ppCodeCxt;
                        fr.GetCodeContext(out ppCodeCxt);
                        IDebugDocumentContext2 ppSrcCxt;
                        ppCodeCxt.GetDocumentContext(out ppSrcCxt);
                        var begPosition = new TEXT_POSITION[1];
                        var endPosition = new TEXT_POSITION[1];
                        ppSrcCxt.GetSourceRange(begPosition, endPosition);
                        string pbstrFileName;
                        ppSrcCxt.GetName(enum_GETNAME_TYPE.GN_NAME, out pbstrFileName);
                        if (m_DTE != null)
                        {
                            m_DTE.Debugger.Breakpoints.Add("", System.IO.Path.GetFileName(pbstrFileName), (int)begPosition[0].dwLine + 1);
                            Breakpoint2 breakpoint2 = m_DTE.Debugger.Breakpoints.Item(m_DTE.Debugger.Breakpoints.Count) as Breakpoint2;
                            breakpoint2.Message = BREAKPOINT_MESSAGE;
                            breakpoint2.BreakWhenHit = false;
                            m_added = true;
                            Output.Write(Output.CONSOLE, "-------------------  AddTracePoint - true");
                        }
                        else
                        {
                            Output.Write(Output.CONSOLE, "-------------------  AddTracePoint - false");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        private void RedirectStdStreams(IDebugThread2 thread)
        {
            Output.Write(Output.CONSOLE, "-------------------  RedirectStdStreams1");
            if (!m_attached && m_added && (thread != null))
            {
                Output.Write(Output.CONSOLE, "-------------------  RedirectStdStreams2");
                if (String.IsNullOrEmpty(m_language) || (m_language != "C#"))
                    return;
                IEnumDebugFrameInfo2 frame;
                thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_LANGUAGE | enum_FRAMEINFO_FLAGS.FIF_FRAME, 0, out frame);
                var frameInfo = new FRAMEINFO[1];
                uint pceltFetched = 0;
                while ((frame.Next(1, frameInfo, ref pceltFetched) == VSConstants.S_OK) && (pceltFetched > 0))
                {
                    var fr = frameInfo[0].m_pFrame as IDebugStackFrame2;
                    if (fr == null)
                    {
                        continue;
                    }
                    IDebugExpressionContext2 expressionContext;
                    fr.GetExpressionContext(out expressionContext);
                    if (expressionContext != null)
                    {
                        IDebugExpression2 de;
                        string error;
                        uint errorCode;
                        if (expressionContext.ParseText(getCommand(), enum_PARSEFLAGS.PARSE_EXPRESSION, 0, out de, out error, out errorCode) == VSConstants.S_OK)
                        {
                            //m_attached = true;
                            IDebugProperty2 dp2;
                            Output.Write(Output.CONSOLE, String.Format("-------------------  RedirectStdStreams3 - {0}",de.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 5000, null, out dp2)));
                        }
                    }
                }
            }
        }
        private string getCommand()
        {
            string result = null;
            var installationPath = (new Uri(typeof(package.VSConsoleOutputPackage).Assembly.CodeBase)).LocalPath;
            if (m_language == "C#")
            {
                installationPath = installationPath.Replace("VsConsoleOutput.dll", "c_sharp.dll");
                installationPath = installationPath.Replace("\\", "\\\\");
                result = "System.Reflection.Assembly.LoadFrom(\"" + installationPath +
                          "\").GetType(\"c_sharp.Redirection\", true, true).GetMethod(\"RedirectToPipe\").Invoke(Activator.CreateInstance(System.Reflection.Assembly.LoadFrom(\"" + installationPath +
                          "\").GetType(\"c_sharp.Redirection\", true, true)), new object[] { });";
            }
            return result;
        }
        private void RemoveBraekpoint()
        {
            Output.Write(Output.CONSOLE, "-------------------  RemoveBraekpoint");
            foreach (Breakpoint2 bp in m_DTE.Debugger.Breakpoints)
            {
                if (bp.Message == BREAKPOINT_MESSAGE)
                {
                    bp.Delete();
                    m_added = false;
                    break;
                }
            }
        }
        private void clear()
        {
            try
            {
                m_attached = false;
                m_added = false;
                m_redirected = false;
                m_ready = false;
                m_entry = false;
                m_was_stoped = false;
                RemoveBraekpoint();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}