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

namespace VsConsoleOutput
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

        private EnvDTE80.Commands2 commands;

        private static DebugManager _instance;
        private static readonly object _padlock = new object();

        public DebugManager()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = VsConsoleOutputPackage.getDTE();
            _debugger = VsConsoleOutputPackage.getDebugger();
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

        private void logbreakpoints()
        {
            foreach (Breakpoint2 breakpoint2 in _dte.Debugger.Breakpoints)
            {
                Output.Log("breakpoint2.FunctionName {0}", breakpoint2.FunctionName);
                Output.Log("breakpoint2.FileLine {0}", breakpoint2.FileLine);
                Output.Log("breakpoint2.FunctionLineOffset {0}", breakpoint2.FunctionLineOffset);
                //tracepoint.Macro = "MACRO";
                //breakpoint2.Message = "Connected";
                //breakpoint2.BreakWhenHit = false;
            }
        }

        private void addMainBreakpoint()
        {
            if (_dte != null)
            {
                _dte.Debugger.Breakpoints.Add("Main");
                foreach (Breakpoint2 breakpoint2 in _dte.Debugger.Breakpoints)
                {
                    if (breakpoint2.FunctionName.Contains("Main("))
                    {
                        breakpoint2.Message = "Connected";
                        breakpoint2.BreakWhenHit = false;
                    }
                }
            }
        }
        private void SetBreakWhenHit(Breakpoint2 breakpoint, bool value)
        {
            var messageStubbed = false;
            if (value && string.IsNullOrEmpty(breakpoint.Message))
            {
                breakpoint.Message = "stub";
                messageStubbed = true;
            }
            breakpoint.BreakWhenHit = value;
            if (messageStubbed)
                breakpoint.Message = "";
        }

        static string GetAssemblyLocalPathFrom(Type type)
        {
            string codebase = type.Assembly.CodeBase;
            var uri = new Uri(codebase, UriKind.Absolute);
            return uri.LocalPath;
        }

        private void RedirectStdStreams(IDebugThread2 thread)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
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
                var fr = frameInfo[0].m_pFrame as IDebugStackFrame2;
                if (fr == null)
                {
                    continue;
                }
                string installationPath = GetAssemblyLocalPathFrom(typeof(VsConsoleOutputPackage));
                installationPath = installationPath.Replace("VsConsoleOutput.dll", "c_sharp.dll");
                installationPath = installationPath.Replace("\\", "\\\\");
                string command = "";

                if (frameInfo[0].m_bstrLanguage == "C#")
                {
                    
                    command = "Console.SetOut((System.IO.StreamWriter)System.Reflection.Assembly.LoadFrom(\"" + installationPath +
                              "\").GetType(\"c_sharp.Redirection\", true, true).GetMethod(\"RedirectToPipe\").Invoke(Activator.CreateInstance(System.Reflection.Assembly.LoadFrom(\"" + installationPath +
                              "\").GetType(\"c_sharp.Redirection\", true, true)), new object[] { }));";
                }
                else //if (frameInfo[0].m_bstrLanguage == "C#")
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

                    if (expressionContext.ParseText(command, enum_PARSEFLAGS.PARSE_EXPRESSION, 0, out de, out error, out errorCode) == VSConstants.S_OK)
                    {
                        isAttached = true;
                        IDebugProperty2 dp2;
                        var res = de.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 5000, null, out dp2);
                        var myInfo = new DEBUG_PROPERTY_INFO[1];
                        dp2.GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL, 0, 5000, null, 0, myInfo);
                        var outputTextWriter = myInfo[0].bstrValue;

                        foreach (Breakpoint2 breakpoint2 in _dte.Debugger.Breakpoints)
                        {
                            if (breakpoint2.FunctionName.Contains("Main("))
                            {
                                breakpoint2.Delete();
                            }
                        }
                    }
                }
            }
        }



        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program,
                            IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {
            if ((thread != null) && (_debug_mode == DBGMODE.DBGMODE_Break) && (!isAttached))
            {
                RedirectStdStreams(thread);
            }
            if ((debugEvent is IDebugSessionCreateEvent2) || (riidEvent.ToString("D") == "2c2b15b7-fc6d-45b3-9622-29665d964a76"))
                Output.Log("debugEvent is IDebugSessionCreateEvent2.{0}", attributes); //"IDebugSessionCreateEvent2","2c2b15b7-fc6d-45b3-9622-29665d964a76"
            else if ((debugEvent is IDebugProcessCreateEvent2) || (riidEvent.ToString("D") == "bac3780f-04da-4726-901c-ba6a4633e1ca"))
                Output.Log("debugEvent is IDebugProcessCreateEvent2.{0}", attributes); //"IDebugProcessCreateEvent2","bac3780f-04da-4726-901c-ba6a4633e1ca"
            else if ((debugEvent is IDebugCustomEvent110) || (riidEvent.ToString("D") == "2615d9bc-1948-4d21-81ee-7a963f20cf59"))
                Output.Log("debugEvent is IDebugCustomEvent110 "); //"IDebugProcessCreateEvent2","2615d9bc-1948-4d21-81ee-7a963f20cf59"
            else if ((debugEvent is IDebugBreakpointErrorEvent2) || (riidEvent.ToString("D") == "abb0ca42-f82b-4622-84e4-6903ae90f210"))
                Output.Log("debugEvent is IDebugBreakpointErrorEvent2 "); //"IDebugProcessCreateEvent2","abb0ca42-f82b-4622-84e4-6903ae90f210"
            else if ((debugEvent is IDebugProgramCreateEvent2) || (riidEvent.ToString("D") == "96cd11ee-ecd4-4e89-957e-b5d496fc4139"))
                Output.Log("debugEvent is IDebugProgramCreateEvent2.{0}", attributes); //"IDebugProgramCreateEvent2","96cd11ee-ecd4-4e89-957e-b5d496fc4139"
            else if ((debugEvent is IDebugModuleLoadEvent2) || (riidEvent.ToString("D") == "989db083-0d7c-40d1-a9d9-921bf611a4b2"))
                Output.Log("debugEvent is IDebugModuleLoadEvent2.{0}", attributes); //"IDebugModuleLoadEvent2","989db083-0d7c-40d1-a9d9-921bf611a4b2"
            else if ((debugEvent is IDebugThreadCreateEvent2) || (riidEvent.ToString("D") == "2090ccfc-70c5-491d-a5e8-bad2dd9ee3ea"))
            {
                //  This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread is created in a program being debugged.
                Output.Log("debugEvent is IDebugThreadCreateEvent2.{0}", attributes); //"IDebugThreadCreateEvent2","2090ccfc-70c5-491d-a5e8-bad2dd9ee3ea"
            }
            else if ((debugEvent is IDebugTelemetryDetailsEvent150) || (riidEvent.ToString("D") == "8f2652b2-cd3c-4aed-a946-a3db6f379412"))
                Output.Log("debugEvent is IDebugTelemetryDetailsEvent150.{0}", attributes); //"IDebugTelemetryDetailsEvent150","8f2652b2-cd3c-4aed-a946-a3db6f379412"
            else if ((debugEvent is IDebugLoadCompleteEvent2) || (riidEvent.ToString("D") == "b1844850-1349-45d4-9f12-495212f5eb0b"))
                Output.Log("debugEvent is IDebugLoadCompleteEvent2.{0}", attributes); //"IDebugLoadCompleteEvent2","b1844850-1349-45d4-9f12-495212f5eb0b"
            else if ((debugEvent is IDebugEntryPointEvent2) || (riidEvent.ToString("D") == "e8414a3e-1642-48ec-829e-5f4040e16da9"))
            {
                // This is place for place initialisation method 
                Output.Log("debugEvent is IDebugEntryPointEvent2"); //"IDebugEntryPointEvent2","e8414a3e-1642-48ec-829e-5f4040e16da9"
                addMainBreakpoint();
            }
            else if (/*(debugEvent is IDebugBreakpointBoundEvent2Guid) || */(riidEvent.ToString("D") == "1dddb704-cf99-4b8a-b746-dabb01dd13a0"))
            {
                Output.Log("debugEvent is IDebugBreakpointBoundEvent2Guid.{0}", attributes); //"IDebugBreakpointBoundEvent2Guid ","1dddb704-cf99-4b8a-b746-dabb01dd13a0"
            }
            else if (/*(debugEvent is IEnumDebugBoundBreakpoints2) || */(riidEvent.ToString("D") == "501c1e21-c557-48b8-ba30-a1eab0bc4a74"))
            {
                Output.Log("debugEvent is IEnumDebugBoundBreakpoints2.{0}", attributes); //"IEnumDebugBoundBreakpoints2","501c1e21-c557-48b8-ba30-a1eab0bc4a74"
            }
            else if ((debugEvent is IDebugCurrentThreadChangedEvent100) || (riidEvent.ToString("D") == "8764364b-0c52-4c7c-af6a-8b19a8c98226"))
                Output.Log("debugEvent is IDebugCurrentThreadChangedEvent100 .{0}", attributes); //"IDebugCurrentThreadChangedEvent100  ","8764364b-0c52-4c7c-af6a-8b19a8c98226"

            else if ((debugEvent is IDebugCurrentThreadChangedEvent100) || (riidEvent.ToString("D") == "8764364b-0c52-4c7c-af6a-8b19a8c98226"))
                Output.Log("debugEvent is IDebugCurrentThreadChangedEvent100 .{0}", attributes); //"IDebugCurrentThreadChangedEvent100  ","8764364b-0c52-4c7c-af6a-8b19a8c98226"

            else if ((debugEvent is IDebugProcessContinueEvent100) || (riidEvent.ToString("D") == "c703ebea-42e7-4768-85a9-692eecba567b"))
            {
                Output.Log("debugEvent is IDebugProcessContinueEvent100.{0}", attributes); //"IDebugProcessContinueEvent100 ","c703ebea-42e7-4768-85a9-692eecba567b"
                //RedirectStdStreams(thread);
            }

            else if ((debugEvent is IDebugThreadDestroyEvent2) || (riidEvent.ToString("D") == "2c3b7532-a36f-4a6e-9072-49be649b8541"))
                Output.Log("debugEvent is IDebugThreadDestroyEvent2.{0}", attributes); //"IDebugThreadDestroyEvent2","2c3b7532-a36f-4a6e-9072-49be649b8541"
            else if ((debugEvent is IDebugProgramDestroyEvent2) || (riidEvent.ToString("D") == "e147e9e3-6440-4073-a7b7-a65592c714b5"))
                Output.Log("debugEvent is IDebugProgramDestroyEvent2.{0}", attributes); //"IDebugProgramDestroyEvent2","e147e9e3-6440-4073-a7b7-a65592c714b5"
            else if ((debugEvent is IDebugProcessDestroyEvent2) || (riidEvent.ToString("D") == "3e2a0832-17e1-4886-8c0e-204da242995f"))
                Output.Log("debugEvent is IDebugProcessDestroyEvent2.{0}", attributes); //"IDebugProcessDestroyEvent2","3e2a0832-17e1-4886-8c0e-204da242995f"
            else if ((debugEvent is IDebugSessionDestroyEvent2) || (riidEvent.ToString("D") == "f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"))
            {
                Output.Log("debugEvent is IDebugSessionDestroyEvent2.{0}", attributes); //"IDebugSessionDestroyEvent2","f199b2c2-88fe-4c5d-a0fd-aa046b0dc0dc"            
                isAttached = false;
            }
            else if ((debugEvent is IDebugMessageEvent2) || (riidEvent.ToString("D") == "3bdb28cf-dbd2-4d24-af03-01072b67eb9e"))
            {
                Output.Log("debugEvent is IDebugMessageEvent2.{0}", attributes); //"IDebugMessageEvent2","3bdb28cf-dbd2-4d24-af03-01072b67eb9e"
                RedirectStdStreams(thread);
            }
            else if ((debugEvent is IDebugProcessDestroyEvent2) || (riidEvent.ToString("D") == "1dddb704-cf99-4b8a-b746-dabb01dd13a0"))
            {
                Output.Log("debugEvent is IDebugBreakpointBoundEvent2.{0}", attributes); //"IDebugBreakpointBoundEvent2","1dddb704-cf99-4b8a-b746-dabb01dd13a0"
            }
            else
                Output.Log("Event Command.name = {0}.{1}", riidEvent.ToString("D"), attributes);
            return VSConstants.S_OK;
        }
    }
}