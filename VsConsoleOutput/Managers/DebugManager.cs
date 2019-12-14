using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace VSConsoleOutputBeta
{    
    internal sealed class DebugManager : IDebugEventCallback2
    {
        public const string BPMessage = "VSOutputConsole connected";

        private DTE m_dte;
        private readonly IVsDebugger m_debugger;
        private bool m_attached;
        private bool m_added;
        private System.Threading.Thread m_serverThread;
        private string m_entryFunctionName;
        private static DebugManager m_instance;
        private static readonly object m_padlock = new object();
        
        public DebugManager()
        {
            m_dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            m_debugger = Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
            m_attached = false;
            m_added = false;
        }

        public static void Instantiate()
        {
            lock (m_padlock)
            {
                if (m_instance != null)
                    throw new InvalidOperationException(string.Format("{0} of Resurrect is already instantiated.", m_instance.GetType().Name));
                m_instance = new DebugManager();
            }
        }

        public static DebugManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    Instantiate();
                }
                return m_instance;
            }
        }

        public void Advise()
        {
            m_debugger.AdviseDebugEventCallback(this);
        }

        public void Unadvise()
        {
            m_debugger.UnadviseDebugEventCallback(this);
        }

        static string GetAssemblyLocalPathFrom(Type type)
        {
            string codebase = type.Assembly.CodeBase;
            var uri = new Uri(codebase, UriKind.Absolute);
            return uri.LocalPath;
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
                    string command = "";
                    string installationPath = GetAssemblyLocalPathFrom(typeof(VSConsoleOutputBetaPackage));
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
                                m_serverThread = new System.Threading.Thread(InputPipe.StartServer);
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
                    thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FUNCNAME, 0, out frame);
                    uint frames;
                    frame.GetCount(out frames);
                    var frameInfo = new FRAMEINFO[1];
                    uint pceltFetched = 0;
                    while ((frame.Next(1, frameInfo, ref pceltFetched) == VSConstants.S_OK) && (pceltFetched > 0))
                    {
                        var fr = frameInfo[0].m_pFrame as IDebugStackFrame2;
                        if (String.IsNullOrEmpty(frameInfo[0].m_bstrFuncName))
                        {
                            continue;
                        }
                        string funcName = frameInfo[0].m_bstrFuncName;
                        m_entryFunctionName = funcName.Substring(funcName.LastIndexOf('.') + 1);

                        if (m_dte != null)
                        {
                            m_dte.Debugger.Breakpoints.Add(m_entryFunctionName);
                            Breakpoint2 breakpoint2 = m_dte.Debugger.Breakpoints.Item(m_dte.Debugger.Breakpoints.Count) as Breakpoint2;
                            breakpoint2.Message = BPMessage;
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
            foreach (Breakpoint2 bp in m_dte.Debugger.Breakpoints)
            {
                if (bp.Message == BPMessage)
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
                OutputText.Write("VSOutputDebugLog", "debugEvent is IDebugEntryPointEvent2");
                AddTracePoint(thread);
            }
            else if ((debugEvent is IDebugSessionDestroyEvent2) || (riidEvent.ToString("D") == "f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"))
            {
                OutputText.Write("VSOutputDebugLog", "debugEvent is IDebugSessionDestroyEvent2"); //"IDebugSessionDestroyEvent2","f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"            
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
                OutputText.Write("VSOutputDebugLog", "debugEvent is IDebugSessionDestroyEvent2"); //"IDebugMessageEvent2","3bdb28cf-dbd2-4d24-af03-01072b67eb9e"
                RedirectStdStreams(thread);
            }
            return VSConstants.S_OK;
        }
    }
}