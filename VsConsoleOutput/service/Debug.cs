using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace service
{
    internal sealed class Debug : IDebugEventCallback2
    {
        public const string BREAKPOINT_MESSAGE = "VSOutputConsole connected";

        private bool m_attached;
        private bool m_added;
        private bool m_break;
        private bool m_redirected;
        private string m_language;
        private System.Threading.Thread m_serverThread;
        private DTE m_DTE;
        private static IVsDebugger s_debugger;
        private static Debug s_Instance;
        private static readonly object s_Padlock = new object();

        public static void Initialize()
        {
            if (s_Instance == null)
            {
                Instantiate();
            }
            s_debugger.AdviseDebugEventCallback(s_Instance);
        }

        public static void Finalize()
        {
            if (s_Instance != null)
            {
                s_debugger.UnadviseDebugEventCallback(s_Instance);
            }
        }

        public Debug()
        {
            m_DTE = Package.GetGlobalService(typeof(SDTE)) as DTE;
            s_debugger = Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
            m_attached = false;
            m_added = false;
            m_break = false;
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

            Output.Write(Output.CONSOLE, String.Format("engine = {0}; process = {1}; program = {2}; thread = {3}",
                engine == null ? "NULL" : "YESS", process == null ? "NULL" : "YESS", program == null ? "NULL" : "YESS", thread == null ? "NULL" : "YESS"));
#endif
            if (m_redirected)
                return VSConstants.S_OK;

            uint suspend;

            if ((thread != null) && (!m_added))
            {
                AddTracePoint(thread);
            }
            if ((debugEvent is IDebugEntryPointEvent2) || (riidEvent.ToString("D") == "e8414a3e-1642-48ec-829e-5f4040e16da9"))
            {
                // This is place for place initialisation method 
                Output.Write(Output.CONSOLE, "debugEvent is IDebugEntryPointEvent2");
                AddTracePoint(thread);
            }
            else if ((debugEvent is IDebugSessionDestroyEvent2) || (riidEvent.ToString("D") == "f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"))
            {
                Output.Write(Output.CONSOLE, "debugEvent is IDebugSessionDestroyEvent2"); //"IDebugSessionDestroyEvent2","f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"            
                if ((m_serverThread != null) && m_serverThread.IsAlive)
                {
                    //m_serverThread.Join();
                    
                }
                m_added = false;
                m_attached = false;
                //RemoveBraekpoint();
            }
            
            else if (m_added)
            {
                if ((debugEvent is IDebugMessageEvent2) || (riidEvent.ToString("D") == "3bdb28cf-dbd2-4d24-af03-01072b67eb9e"))
                {
                    Output.Write(Output.CONSOLE, "debugEvent is IDebugMessageEvent2"); 
                    RedirectStdStreams(thread);
                }
                else if ((debugEvent is IDebugThreadCreateEvent2) || (riidEvent.ToString("D") == "3bdb28cf-dbd2-4d24-af03-01072b67eb9e"))
                {
                    Output.Write(Output.CONSOLE, "debugEvent is IDebugThreadCreateEvent2"); 
                    RedirectStdStreams(thread);
                }
            }
            
            return VSConstants.S_OK;

            if (!m_added && (thread != null))
            {
                m_added = true;
                //thread.Suspend(out suspend);
                AddTracePoint(thread);
            }

            //if (!m_break && m_added && (engine == null) && (process == null) && (program == null) && (thread == null))
            //{
            //    m_break = true;
            //    m_DTE.ExecuteCommand("Debug.BreakAll");
            //}

            if (m_break && m_added && (debugEvent is IDebugBreakEvent2))
            {
                //thread.Resume(out suspend);
                RedirectStdStreams(thread);
            }
            //else if (m_attached && (debugEvent is IDebugThreadSuspendChangeEvent100))
            //{
            //    m_DTE.ExecuteCommand("Debug.Start");
            //    RemoveBraekpoint();
            //    m_redirected = true;
            //}
            else if(debugEvent is IDebugProgramDestroyEvent2)
            {
                clear();
            }
            return VSConstants.S_OK;
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

        private void RedirectStdStreams(IDebugThread2 thread)
        {
            Output.Write(Output.CONSOLE, "RedirectStdStreams");
            if (!m_attached && m_added && (thread != null))
            {
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
                            m_attached = true;
                            IDebugProperty2 dp2;
                            de.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 5000, null, out dp2);
                        }
                    }
                }
            }
        }

        private void AddTracePoint(IDebugThread2 thread)
        {
            Output.Write(Output.CONSOLE, "AddTracePoint");
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
                            m_serverThread = new System.Threading.Thread(output.Pipe.StartServer);
                            m_serverThread.Start();
                            m_DTE.Debugger.Breakpoints.Add("", System.IO.Path.GetFileName(pbstrFileName), (int)begPosition[0].dwLine + 1);
                            Breakpoint2 breakpoint2 = m_DTE.Debugger.Breakpoints.Item(m_DTE.Debugger.Breakpoints.Count) as Breakpoint2;
                            breakpoint2.Message = BREAKPOINT_MESSAGE;
                            breakpoint2.BreakWhenHit = false;
                            m_added = true;
                        }   
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void RemoveBraekpoint()
        {
            Output.Write(Output.CONSOLE, "RemoveBraekpoint");
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
            Output.Write(Output.CONSOLE, "clear");
            try
            {
                m_attached = false;
                m_added = false;
                m_break = false;
                m_redirected = false;
                RemoveBraekpoint();
                if ((m_serverThread != null) && (m_serverThread.IsAlive))
                {
                    m_serverThread.Join();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}