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
        private string m_language;
        private static uint _cookie;
        private System.Threading.Thread m_serverThread;
        private DTE m_DTE;
        private static IVsDebugger s_debugger;
        private static Debug s_Instance;
        private static readonly object s_Padlock = new object();

        public int OnModeChange(DBGMODE mode)
        {
            if (mode  == DBGMODE.DBGMODE_Run)
            {
                m_redirected = false;
                clear();
                if ((m_serverThread == null) || !m_serverThread.IsAlive)
                {
                    m_serverThread = new System.Threading.Thread(output.Pipe.StartServer);
                    m_serverThread.Start();
                }
            }
            return VSConstants.S_OK;
        }
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
            if (debugEvent is IDebugSessionDestroyEvent2)
            {
                clear();
            }
            if (debugEvent is IDebugSessionCreateEvent2)
            {
                Output.ClearPane(Output.CONSOLE);
            }

            if (m_redirected)
            {
                return VSConstants.S_OK;
            }

            if (m_attached)
            {
                RemoveBraekpoint();
                m_redirected = true;
            }
            if (thread != null)
            {
                AddTracePoint(thread);
                RedirectStdStreams(thread);
            }

            
            return VSConstants.S_OK;

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
                            IDebugProperty2 dp2;
                           de.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 5000, null, out dp2);
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
            foreach (Breakpoint2 bp in m_DTE.Debugger.Breakpoints)
            {
                if (bp.Message == BREAKPOINT_MESSAGE)
                {
                    bp.Delete();
                    m_added = false;
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
                RemoveBraekpoint();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}