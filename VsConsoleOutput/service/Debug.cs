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

        private DTE m_DTE;
        private static IVsDebugger s_debugger;
        private bool m_attached;
        private bool m_added;
        private System.Threading.Thread m_serverThread;
        private string m_EntryFunction;
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

        static string GetPackagePath(Type type)
        {
            var a_Result = new Uri(type.Assembly.CodeBase, UriKind.Absolute);
            return a_Result.LocalPath;
        }

        private void RedirectStdStreams(IDebugThread2 thread)
        {
            if (!m_attached && (thread != null))
            {
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
                    var command = "";
                    var installationPath = GetPackagePath(typeof(package.VSConsoleOutputPackage));
                    if (frameInfo[0].m_bstrLanguage == "C#")
                    {
                        installationPath = installationPath.Replace("VsConsoleOutput.dll", "c_sharp.dll");
                        installationPath = installationPath.Replace("\\", "\\\\");
                        command = "System.Reflection.Assembly.LoadFrom(\"" + installationPath +
                                  "\").GetType(\"c_sharp.Redirection\", true, true).GetMethod(\"RedirectToPipe\").Invoke(Activator.CreateInstance(System.Reflection.Assembly.LoadFrom(\"" + installationPath +
                                  "\").GetType(\"c_sharp.Redirection\", true, true)), new object[] { });";
                        IDebugExpressionContext2 expressionContext;
                        fr.GetExpressionContext(out expressionContext);
                        if (expressionContext != null)
                        {
                            IDebugExpression2 de;
                            string error;
                            uint errorCode;

                            if (expressionContext.ParseText(command, enum_PARSEFLAGS.PARSE_EXPRESSION, 0, out de, out error, out errorCode) == VSConstants.S_OK)
                            {
                                //(Pipe.Type.INPUT);
                                m_serverThread = new System.Threading.Thread(output.Pipe.StartServer);
                                m_serverThread.Start();
                                m_attached = true;
                                IDebugProperty2 dp2;
                                de.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 5000, null, out dp2);
                                RemoveBraekpoint();
                            }
                        }
                    }
                }
            }
        }

        private void AddTracePoint(IDebugThread2 thread)
        {
            try
            {
                if ((thread != null) && (!m_added))
                {
                    IEnumDebugFrameInfo2 frame;
                    thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FUNCNAME | enum_FRAMEINFO_FLAGS.FIF_LANGUAGE, 0, out frame);
                    uint frames;
                    frame.GetCount(out frames);
                    var frameInfo = new FRAMEINFO[1];
                    uint pceltFetched = 0;
                    while ((frame.Next(1, frameInfo, ref pceltFetched) == VSConstants.S_OK) && (pceltFetched > 0))
                    {
                        if (frameInfo[0].m_bstrLanguage != "C#")
                        {
                            return;
                        }
                        var fr = frameInfo[0].m_pFrame as IDebugStackFrame2;
                        if (String.IsNullOrEmpty(frameInfo[0].m_bstrFuncName))
                        {
                            continue;
                        }
                        string funcName = frameInfo[0].m_bstrFuncName;
                        m_EntryFunction = funcName.Substring(funcName.LastIndexOf('.') + 1);
                        if (m_DTE != null)
                        {
                            m_DTE.Debugger.Breakpoints.Add(m_EntryFunction);
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

        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program,
                            IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {
            if ((debugEvent is IDebugEntryPointEvent2) || (riidEvent.ToString("D") == "e8414a3e-1642-48ec-829e-5f4040e16da9"))
            {
                // This is place for place initialisation method 
                Output.Write("VSOutputDebugLog", "debugEvent is IDebugEntryPointEvent2");
                AddTracePoint(thread);
            }
            else if ((debugEvent is IDebugSessionDestroyEvent2) || (riidEvent.ToString("D") == "f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"))
            {
                Output.Write("VSOutputDebugLog", "debugEvent is IDebugSessionDestroyEvent2"); //"IDebugSessionDestroyEvent2","f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"            
                if ((m_serverThread != null) && m_serverThread.IsAlive)
                {
                    m_serverThread.Join();
                    m_added = false;
                }
                m_attached = false;
                RemoveBraekpoint();
            }
            else if ((debugEvent is IDebugMessageEvent2) || (riidEvent.ToString("D") == "3bdb28cf-dbd2-4d24-af03-01072b67eb9e"))
            {
                Output.Write("VSOutputDebugLog", "debugEvent is IDebugSessionDestroyEvent2"); //"IDebugMessageEvent2","3bdb28cf-dbd2-4d24-af03-01072b67eb9e"
                RedirectStdStreams(thread);
            }
            return VSConstants.S_OK;
        }
    }
}