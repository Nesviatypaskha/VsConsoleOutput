using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace service
{
    internal sealed class Debug : IDebugEventCallback2, IVsDebuggerEvents
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private static bool s_IsInitialized = false;
        private static MODULE_INFO[] s_Module = null;
        private static string s_Path = "";
        private static System.Threading.Thread s_Thread;
        private static IVsDebugger s_Service = null;
        private static Debug s_Instance = null;
        private static uint s_Cookie = 0;
        private static DTE s_DTE;

        private const string BREAKPOINT_MESSAGE = "VSOutputConsole connected";

        public static void Initialize()
        {
            {
                s_Service = Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
                s_DTE = Package.GetGlobalService(typeof(SDTE)) as DTE;
            }
            if (s_Service != null)
            {
                s_Service.AdviseDebuggerEvents(Instance, out s_Cookie);
                s_Service.AdviseDebugEventCallback(Instance);
            }
        }

        public static void Finalize()
        {
            if (s_Service != null)
            {
                s_Service.UnadviseDebuggerEvents(s_Cookie);
                s_Service.UnadviseDebugEventCallback(Instance);
            }
        }

        public static Debug Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new Debug();
                }
                return s_Instance;
            }
        }

        public int OnModeChange(DBGMODE mode)
        {
            if (mode == DBGMODE.DBGMODE_Run)
            {
                {
                    Output.Clear();
                }
                if ((s_Thread == null) || !s_Thread.IsAlive)
                {
                    s_Thread = new System.Threading.Thread(output.Pipe.StartServer);
                    s_Thread.Start();
                }
            }
            return VSConstants.S_OK;
        }

        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program, IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {
            try
            {
                __Event(engine, process, program, thread, debugEvent, ref riidEvent, attributes);
                if (debugEvent is IDebugModuleLoadEvent2)
                {
                    var a_Context1 = __GetModule(debugEvent as IDebugModuleLoadEvent2);
                    if (a_Context1 != null)
                    {
                        var a_Context2 = a_Context1[0].m_bstrName.ToLower();
                        if ((a_Context2 == "kernel32.dll") || (a_Context2 == "system.private.corelib.dll"))
                        {
                            s_Module = a_Context1;
                        }
                    }
                }
                if (debugEvent is IDebugProgramCreateEvent2)
                {
                    if (s_IsInitialized)
                    {
                        s_IsInitialized = false;
                        s_Module = null;
                    }
                    return VSConstants.S_OK;
                }
                if (debugEvent is IDebugProgramDestroyEvent2)
                {
                    {
                        s_IsInitialized = false;
                    }
                    return VSConstants.S_OK;
                }
                if (debugEvent is IDebugEntryPointEvent2)
                {
                    if ((s_Module != null) && (s_Module[0].m_bstrName.ToLower() == "system.private.corelib.dll"))
                    {
                        __AddTracePoint(thread);
                    }
                    else if (s_IsInitialized == false)
                    {
                        s_IsInitialized = true;
                        {
                            __Execute(thread, __GetLibraryName());
                            __Execute(thread, __GetFunctionName());
                        }
                    }
                    return VSConstants.S_OK;
                }
                if (debugEvent is IDebugMessageEvent2)
                {
                    if (s_IsInitialized == false)
                    {
                        s_IsInitialized = true;
                        {
                            __Execute(thread, __GetLibraryName());
                            __RemoveBraekpoint();
                        }
                    }
                    return VSConstants.S_OK;
                }
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
            return VSConstants.S_OK;
        }

        private void __Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program, IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {
            if (debugEvent is IDebugSessionCreateEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugSessionCreateEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugProcessCreateEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugProcessCreateEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugCustomEvent110)
                Output.WriteLine(String.Format("debugEvent is IDebugCustomEvent110.{0}\n", attributes));
            else if (debugEvent is IDebugProgramCreateEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugProgramCreateEvent2.{0}\n", attributes));
            else
            if (debugEvent is IDebugModuleLoadEvent2)
            {
                Output.WriteLine(String.Format("debugEvent is IDebugModuleLoadEvent2.{0}               - \n", attributes));
                var debug_event = debugEvent as IDebugModuleLoadEvent2;
                IDebugModule2 debugModule;
                string strDebugMessage = string.Empty;
                int load = 0;
                debug_event.GetModule(out debugModule, ref strDebugMessage, ref load);
                var info = new MODULE_INFO[1];
                debugModule.GetInfo(enum_MODULE_INFO_FIELDS.MIF_ALLFIELDS, info);
                Output.WriteLine(String.Format("name = {0};  \n", info[0].m_bstrName));
            }
            else if (debugEvent is IDebugThreadCreateEvent2)
                //  This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread is created in a program being debugged.
                Output.WriteLine(String.Format("debugEvent is IDebugThreadCreateEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugTelemetryDetailsEvent150)
                Output.WriteLine(String.Format("debugEvent is IDebugTelemetryDetailsEvent150.{0}\n", attributes));
            else if (debugEvent is IDebugLoadCompleteEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugLoadCompleteEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugEntryPointEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugEntryPointEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugProcessContinueEvent100)
                Output.WriteLine(String.Format("debugEvent is IDebugProcessContinueEvent100.{0}\n", attributes));
            else if (debugEvent is IDebugThreadDestroyEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugThreadDestroyEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugProgramDestroyEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugProgramDestroyEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugProcessDestroyEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugProcessDestroyEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugSessionDestroyEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugSessionDestroyEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugBreakpointErrorEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugBreakpointErrorEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugBreakpointBoundEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugBreakpointBoundEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugMessageEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugMessageEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugOutputStringEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugOutputStringEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugExceptionEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugExceptionEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugCurrentThreadChangedEvent100)
                Output.WriteLine(String.Format("debugEvent is IDebugCurrentThreadChangedEvent100.{0}\n", attributes));
            else if (debugEvent is IDebugBreakEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugBreakEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugBreakpointEvent2)
                Output.WriteLine(String.Format("debugEvent is IDebugBreakpointEvent2.{0}\n", attributes));
            else if (debugEvent is IDebugThreadSuspendChangeEvent100)
                Output.WriteLine(String.Format("debugEvent is IDebugThreadSuspendChangeEvent100.{0}\n", attributes));
            else
                Output.WriteLine(String.Format("Event Command.name = {0}.{1}\n", riidEvent.ToString("B"), attributes));
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
            Output.WriteLine(String.Format("engine = {0}; process = {1}; program = {2}; thread = {3}, threadstate = {4}\n",
                    engine == null ? "NULL" : "YESS", process == null ? "NULL" : "YESS", program == null ? "NULL" : "YESS", thread == null ? "NULL" : "YESS", threadstate));
        }
        private void __AddTracePoint(IDebugThread2 thread)
        {
            try
            {
                if (thread != null)
                {
                    IEnumDebugFrameInfo2 frame;
                    thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FUNCNAME, 0, out frame);
                    var frameInfo = new FRAMEINFO[1];
                    uint pceltFetched = 0;
                    while ((frame.Next(1, frameInfo, ref pceltFetched) == VSConstants.S_OK) && (pceltFetched > 0))
                    {
                        if (String.IsNullOrEmpty(frameInfo[0].m_bstrFuncName))
                        {
                            continue;
                        }
                        if (s_DTE != null)
                        {
                            s_DTE.Debugger.Breakpoints.Add(frameInfo[0].m_bstrFuncName);
                            Breakpoint2 breakpoint2 = s_DTE.Debugger.Breakpoints.Item(s_DTE.Debugger.Breakpoints.Count) as Breakpoint2;
                            breakpoint2.Message = BREAKPOINT_MESSAGE;
                            breakpoint2.BreakWhenHit = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
        }
        private void __RemoveBraekpoint()
        {
            foreach (Breakpoint2 bp in s_DTE.Debugger.Breakpoints)
            {
                if (bp.Message == BREAKPOINT_MESSAGE)
                {
                    bp.Delete();
                }
            }
        }
        private static string __GetLibraryName()
        {
            var a_Result = __GetPath();
            if ((s_Module != null) )
            {
                if (s_Module[0].m_bstrName.ToLower() == "kernel32.dll")
                {
                    var a_Context1 = GetProcAddress((IntPtr)(s_Module[0].m_addrLoadAddress), "LoadLibraryA");
                    a_Result += "\\console.proxy.cpp.dll";
                    a_Result = a_Result.Replace("\\", "\\\\");
                    var asdf = a_Context1.ToString("X");
                    a_Result = "((int (__stdcall *)(const char*))0x" + a_Context1.ToString("X") + ")(\"" + a_Result + "\")";
                }
                else
                {
                    a_Result += "\\console.proxy.cs.dll";
                    a_Result = a_Result.Replace("\\", "\\\\");
                    a_Result = "System.Reflection.Assembly.LoadFrom(\"" + a_Result + "\").GetType(\"proxy.Redirection\").GetMethod(\"Connect\").Invoke(null, null)";
                }
            }
            else
            {
                a_Result += "\\console.proxy.cs.dll";
                a_Result = a_Result.Replace("\\", "\\\\");
                a_Result = "System.Reflection.Assembly.LoadFrom(\"" + a_Result + "\")";
            }
            return a_Result;
        }

        private static string __GetFunctionName()
        {
            return (s_Module != null) ? "" : "proxy.Redirection.Connect()";
        }

        private static string __GetPath()
        {
            if (string.IsNullOrEmpty(s_Path))
            {
                s_Path = Path.GetDirectoryName((new Uri(typeof(package.VSConsoleOutputPackage).Assembly.CodeBase)).LocalPath);
            }
            return s_Path;
        }

        private static MODULE_INFO[] __GetModule(IDebugModuleLoadEvent2 debugEvent)
        {
            if (debugEvent != null)
            {
                var a_Context = (IDebugModule2)null;
                {
                    var a_Context1 = "";
                    var a_Context2 = 0;
                    if (debugEvent.GetModule(out a_Context, ref a_Context1, ref a_Context2) != VSConstants.S_OK)
                    {
                        return null;
                    }
                }
                {
                    var a_Result = new MODULE_INFO[1];
                    if (a_Context.GetInfo(enum_MODULE_INFO_FIELDS.MIF_ALLFIELDS, a_Result) == VSConstants.S_OK)
                    {
                        return a_Result;
                    }
                }
            }
            return null;
        }

        private static void __Execute(IDebugThread2 thread, string expression)
        {
            try
            {
                if (thread != null)
                {
                    var a_Context = (IEnumDebugFrameInfo2)null;
                    if (thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_LANGUAGE | enum_FRAMEINFO_FLAGS.FIF_FRAME, 0, out a_Context) == VSConstants.S_OK)
                    {
                        var a_Context1 = new FRAMEINFO[1];
                        var a_Context2 = (uint)0;
                        while ((a_Context.Next(1, a_Context1, ref a_Context2) == VSConstants.S_OK) && (a_Context2 > 0))
                        {
                            var a_Context3 = (IDebugExpressionContext2)null;
                            var a_Context4 = a_Context1[0].m_pFrame as IDebugStackFrame2;
                            if (a_Context4 != null)
                            {
                                a_Context4.GetExpressionContext(out a_Context3);
                            }
                            if (a_Context3 != null)
                            {
                                __Execute(a_Context3, expression);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
        }

        private static void __Execute(IDebugExpressionContext2 context, string expression)
        {
            if ((context != null) && (string.IsNullOrEmpty(expression) == false))
            {
                var a_Context = (IDebugExpression2)null;
                {
                    var a_Context1 = "";
                    var a_Context2 = (uint)0;
                    if (context.ParseText(expression, enum_PARSEFLAGS.PARSE_EXPRESSION, 0, out a_Context, out a_Context1, out a_Context2) == VSConstants.S_OK)
                    {
                        var a_Context3 = (IDebugProperty2)null;
                        a_Context.EvaluateSync(enum_EVALFLAGS.EVAL_RETURNVALUE, 2000, null, out a_Context3);
                    }
                    else
                    {
                        service.Output.WriteError(a_Context1 + "// Error code: " + a_Context2.ToString());
                    }
                }
            }
        }
    }
}
