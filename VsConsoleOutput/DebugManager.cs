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
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;

namespace VSConsoleOutputBeta
{
    internal sealed class DebugManager : IVsDebuggerEvents, IDebugEventCallback2
    {
        private DTE _dte;
        private readonly IVsDebugger _debugger;
        private readonly IVsDebugger2 _debugger2;
        private IVsSolutionBuildManager _solutionBuildManager;
        private uint _cookie;
        private DBGMODE _debug_mode;
        private bool isAttached;
        private bool added;
        private const string bpMessage = "VSOutputConsole connected";
        //private System.Threading.Thread clientThread;
        private System.Threading.Thread serverThread;

        private string entryFunctionName;

        private static DebugManager _instance;
        private static readonly object _padlock = new object();

        public DebugManager()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = VSConsoleOutputBetaPackage.getDTE();
            _debugger = VSConsoleOutputBetaPackage.getDebugger();
            isAttached = false;
            added = false;
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
            _debug_mode = mode;
            return VSConstants.S_OK;
        }

        static string GetAssemblyLocalPathFrom(Type type)
        {
            string codebase = type.Assembly.CodeBase;
            var uri = new Uri(codebase, UriKind.Absolute);
            return uri.LocalPath;
        }

        private void RedirectStdStreams(IDebugThread2 thread)
        {
            if (!isAttached && (thread != null))
            {
                IEnumDebugFrameInfo2 frame;
                thread.EnumFrameInfo(
                    enum_FRAMEINFO_FLAGS.FIF_LANGUAGE |
                    enum_FRAMEINFO_FLAGS.FIF_FRAME |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE |
                    enum_FRAMEINFO_FLAGS.FIF_ARGS |
                    enum_FRAMEINFO_FLAGS.FIF_MODULE |
                    enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO |
                    enum_FRAMEINFO_FLAGS.FIF_STALECODE |
                    enum_FRAMEINFO_FLAGS.FIF_FLAGS |
                    enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_FORMAT |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LINES |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_OFFSET |
                    enum_FRAMEINFO_FLAGS.FIF_FILTER_INCLUDE_ALL |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_NAMES |
                    enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_ALL |
                    enum_FRAMEINFO_FLAGS.FIF_ARGS_TYPES |
                    enum_FRAMEINFO_FLAGS.FIF_ARGS_NAMES |
                    enum_FRAMEINFO_FLAGS.FIF_ARGS_VALUES |
                    enum_FRAMEINFO_FLAGS.FIF_ARGS_ALL
                    , 0, out frame);
                uint frames;
                frame.GetCount(out frames);
                var frameInfo = new FRAMEINFO[1];
                uint pceltFetched = 0;
                while ((frame.Next(1, frameInfo, ref pceltFetched) == VSConstants.S_OK) && (pceltFetched > 0))
                {
                    if (isAttached)
                        break;
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

                    }
                    else // if (frameInfo[0].m_bstrLanguage == "C++")
                    {
                        return;
                    }

                    IDebugExpressionContext2 expressionContext;
                    fr.GetExpressionContext(out expressionContext);
                    if (expressionContext != null)
                    {
                        IDebugExpression2 de;
                        string error;
                        uint errorCode;

                        if (expressionContext.ParseText(command, enum_PARSEFLAGS.PARSE_EXPRESSION, 0, out de, out error, out errorCode) == VSConstants.S_OK)
                        {
                            isAttached = true;
                            added = false;
                            serverThread = new System.Threading.Thread(Pipes.StartServer);
                            serverThread.Start();
                            Output.Console("VSOutputConsole ready");
                            IDebugProperty2 dp2;
                            var res = de.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 5000, null, out dp2);
                            var myInfo = new DEBUG_PROPERTY_INFO[1];
                            dp2.GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL, 0, 5000, null, 0, myInfo);
                            var outputTextWriter = myInfo[0].bstrValue;

                            foreach (Breakpoint2 bp in _dte.Debugger.Breakpoints)
                            {
                                if (bp.Message == bpMessage)
                                {
                                    bp.Delete();
                                    break;
                                }
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
                if ((thread != null) && (!added))
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
                        entryFunctionName = funcName.Substring(funcName.LastIndexOf('.') + 1);

                        if (_dte != null)
                        {
                            _dte.Debugger.Breakpoints.Add(entryFunctionName);
                            Breakpoint2 breakpoint2 = _dte.Debugger.Breakpoints.Item(_dte.Debugger.Breakpoints.Count) as Breakpoint2;
                            breakpoint2.Message = bpMessage;
                            breakpoint2.BreakWhenHit = false;
                            added = true;
                        }   
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program,
                            IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {
         
            if ((debugEvent is IDebugEntryPointEvent2) || (riidEvent.ToString("D") == "e8414a3e-1642-48ec-829e-5f4040e16da9"))
            {
                // This is place for place initialisation method 
                Output.Log("debugEvent is IDebugEntryPointEvent2"); //"IDebugEntryPointEvent2","e8414a3e-1642-48ec-829e-5f4040e16da9"
                AddTracePoint(thread);
            }
            else if ((debugEvent is IDebugProcessContinueEvent100) || (riidEvent.ToString("D") == "c703ebea-42e7-4768-85a9-692eecba567b"))
            {
                Output.Log("debugEvent is IDebugProcessContinueEvent100.{0}", attributes); //"IDebugProcessContinueEvent100 ","c703ebea-42e7-4768-85a9-692eecba567b"
                //RedirectStdStreams(thread);
            }
            else if ((debugEvent is IDebugSessionDestroyEvent2) || (riidEvent.ToString("D") == "f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"))
            {
                Output.Log("debugEvent is IDebugSessionDestroyEvent2.{0}", attributes); //"IDebugSessionDestroyEvent2","f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"            
                if ((serverThread != null) && serverThread.IsAlive)
                {
                    serverThread.Join();
                    added = false;
                }
                isAttached = false;
            }
            else if ((debugEvent is IDebugMessageEvent2) || (riidEvent.ToString("D") == "3bdb28cf-dbd2-4d24-af03-01072b67eb9e"))
            {
                Output.Log("debugEvent is IDebugMessageEvent2.{0}", attributes); //"IDebugMessageEvent2","3bdb28cf-dbd2-4d24-af03-01072b67eb9e"
                RedirectStdStreams(thread);
            }
            return VSConstants.S_OK;
        }
    }
}