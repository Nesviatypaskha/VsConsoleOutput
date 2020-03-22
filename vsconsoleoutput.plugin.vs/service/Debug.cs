using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;

namespace service
{
    internal sealed class Debug : IDebugEventCallback2, IVsDebuggerEvents
    {
        private static bool s_IsInitialized = false;
        private static MODULE_INFO[] s_Module = null;
        private static string s_Path = "";
        private static System.Threading.Thread s_Thread;
        private static IVsDebugger s_Service = null;
        private static Debug s_Instance = null;
        private static uint s_Cookie = 0;
        
        public static void Initialize()
        {
            {
                s_Service = Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
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
                if (debugEvent is IDebugModuleLoadEvent2)
                {
                    var a_Context = __GetModule(debugEvent as IDebugModuleLoadEvent2);
                    if (a_Context != null)
                    {
                        if (a_Context[0].m_bstrName.ToLower() == "kernel32.dll")
                        {
                            s_Module = a_Context;
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
                    if (s_IsInitialized == false)
                    {
                        s_IsInitialized = true;
                        __Execute(thread, __GetLibraryName());
                        __Execute(thread, __GetFunctionName());
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

        private static string __GetLibraryName()
        {
            var a_Result = __GetPath();
            if (s_Module != null)
            {
                a_Result += "\\console.proxy.cpp.dll";
                a_Result = a_Result.Replace("\\", "\\\\");
                a_Result = "((int (__stdcall *)(const char*))0x77a02990)(\"" + a_Result + "\")";
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
