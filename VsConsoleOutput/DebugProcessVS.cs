using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsConsoleOutput
{

    // TODO: DebugSetProcessKillOnExit
    // https://docs.microsoft.com/ru-ru/windows/console/reading-input-buffer-events
    // 

    internal class DebugProcessVS
    {
        public static DTE2 dte;
        private System.Diagnostics.Process debuggedProcess;

        public void StartProcess(string exePath, string args, string workDir)
        {
            try
            {
                if (debuggedProcess != null)
                {
                    Output.Log("Old debuggedProcess killed {0}", debuggedProcess.Id);
                    debuggedProcess.Kill();
                }

                if (!File.Exists(exePath))
                {
                    Output.Log("Executable not found: {0}", exePath);
                }
                else
                {
                    Output.ActivateConsole();
                    Output.ClearConsole();
                    this.debuggedProcess = new System.Diagnostics.Process();
                    this.debuggedProcess.StartInfo.FileName = exePath;
                    this.debuggedProcess.StartInfo.WorkingDirectory = workDir;
                    this.debuggedProcess.StartInfo.Arguments = args.Trim();
                    this.debuggedProcess.EnableRaisingEvents = true;
                    this.debuggedProcess.StartInfo.UseShellExecute = false;
                    //this.debuggedProcess.StartInfo.ErrorDialog = false;
                    this.debuggedProcess.StartInfo.RedirectStandardOutput = true;
                    this.debuggedProcess.StartInfo.RedirectStandardError = true;
                    //this.debuggedProcess.StartInfo.RedirectStandardInput = true;
                    // TODO: StandardOutputEncoding

                    this.debuggedProcess.StartInfo.CreateNoWindow = true;
                    this.debuggedProcess.Exited += new EventHandler(this.Process_Exited);
                    this.debuggedProcess.OutputDataReceived += new DataReceivedEventHandler(this.StandardOutputReceiver);
                    this.debuggedProcess.ErrorDataReceived += new DataReceivedEventHandler(this.StandardErrorReceiver);
                    //this.debuggedProcess.ErrorDataReceived += new DataReceivedEventHandler(this.StandardErrorReceiver);
                    //WriteToStandardInput(debuggedProcess);
                    // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.datareceivedeventhandler?view=netframework-4.8
                    // https://stackoverflow.com/questions/7850352/how-can-i-copy-the-stdout-of-a-process-copy-not-redirect
                    // http://vsokovikov.narod.ru/New_MSDN_API/Process_thread/child_process_redirect_io.htm
                    bool started = false;
                    started = this.debuggedProcess.Start();
                    if (started)
                    {
                        this.debuggedProcess.BeginOutputReadLine();
                        this.debuggedProcess.BeginErrorReadLine();
                        Output.Log("Started file: {0}" + this.debuggedProcess.StartInfo.FileName);
                        Attach(debuggedProcess);
                    }
                    else
                    {
                        Output.Log("NOT STARTED file: {0}" + this.debuggedProcess.StartInfo.FileName);
                    }

                    // TODO: duplicate 
                    //StreamReader outputStream = debuggedProcess.StandardOutput;
                }
            }
            catch (Exception ex)
            {
                Output.Log("EXCEPTION: Could not start executable {0} {1}", exePath, ex.ToString());
            }
        }
        public void Process_Exited(object sender, EventArgs e)
        {
            try
            {
                string in_info = string.Format("Exit time:    {0} Exit code:    {1}", (object)this.debuggedProcess.ExitTime, (object)this.debuggedProcess.ExitCode);
                TimeSpan in_executionTime = this.debuggedProcess.ExitTime - this.debuggedProcess.StartTime;
            }
            catch (Exception ex)
            {
                Output.Log("EXCEPTION: within Process_Exited:  {0} {1}", debuggedProcess.ProcessName, ex.ToString());
            }
        }
        public void CloseProcess()
        {
            try
            {
                if (this.debuggedProcess == null)
                    return;
                Output.Log("Closing process {0}" + this.debuggedProcess.ProcessName);
                this.debuggedProcess.Close();
            }
            catch (Exception ex)
            {
                Output.Log("EXCEPTION: within Closing process:  {0} {1}", debuggedProcess.ProcessName, ex.ToString());
            }
        }
        public void KillProcess()
        {
            try
            {
                if (this.debuggedProcess == null)
                    return;
                Output.Log("Killing process {0}" + this.debuggedProcess.ProcessName);
                this.debuggedProcess.Kill();
            }
            catch (Exception ex)
            {
                Output.Log("EXCEPTION: within Killing process:  {0} {1}", debuggedProcess.ProcessName, ex.ToString());
            }
        }
        private void StandardOutputReceiver(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrEmpty(outLine.Data))
                return;
            Output.Console("Output>{0}", outLine.Data);
        }
        private void StandardErrorReceiver(object sendingProcess, DataReceivedEventArgs errLine)
        {
            if (string.IsNullOrEmpty(errLine.Data))
                return;
            Output.Console("Error >{0}", errLine.Data);
        }

        public static void Attach(System.Diagnostics.Process process, int maxTries = 5)
        {
            if (dte == null)
            {
                Output.Log("No debugger found, nothing attached...");
                return;
            }

            // Try loop - visual studio may not respond the first time.
            // We also don't want it to stall the main thread
            new System.Threading.Thread(() =>
            {
                while (maxTries-- > 0)
                {
                    try
                    {
                        Processes processes = dte.Debugger.LocalProcesses;
                        foreach (EnvDTE.Process proc in processes)
                        {
                            try
                            {
                                if (proc.Name.Contains(process.ProcessName))
                                {
                                    proc.Attach();
                                    Output.Log("Attatched to process {0} successfully.", process.ProcessName);
                                    return;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                    // Wait for debugger and application and debugger to find application
                    System.Threading.Thread.Sleep(1500);
                }
            }).Start();
        }

        //https://gitter.im/Microsoft/VSProjectSystem/archives/2017/09/27?at=59cb8d6f177fb9fe7e0b15cb
        //public override async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions)
        //{
        //    var settings = new DebugLaunchSettings(launchOptions);

        //    // The properties that are available via DebuggerProperties are determined by the property XAML files in your project.
        //    settings.Executable = @"C:\Users\Igal\AppData\Roaming\scriptcs\scriptcs.exe";

        //    settings.Arguments = @"D:\code\ScriptCSApp5\ScriptCSApp5\app.csx";
        //    settings.LaunchOperation = DebugLaunchOperation.CreateProcess;

        //    return new IDebugLaunchSettings[] { settings };
        //}
    }
}
