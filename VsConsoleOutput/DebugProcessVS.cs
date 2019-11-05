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
    internal class DebugProcessVS
    {
        public static DTE2 dte;
        private System.Diagnostics.Process debuggedProcess;

        public void StartProcess(string exePath, string args, string workDir)
        {
            try
            {
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
                    this.debuggedProcess.Exited += new EventHandler(this.Process_Exited);
                    this.debuggedProcess.EnableRaisingEvents = true;
                    this.debuggedProcess.StartInfo.UseShellExecute = true;
                    this.debuggedProcess.StartInfo.RedirectStandardOutput = true;
                    this.debuggedProcess.StartInfo.RedirectStandardError = true;
                    this.debuggedProcess.StartInfo.RedirectStandardInput = true;
                    // TODO: StandardOutputEncoding

                    this.debuggedProcess.StartInfo.CreateNoWindow = true;
                    this.debuggedProcess.OutputDataReceived += new DataReceivedEventHandler(this.StandardOutputReceiver);
                    this.debuggedProcess.ErrorDataReceived += new DataReceivedEventHandler(this.StandardErrorReceiver);
                    //this.debuggedProcess.ErrorDataReceived += new DataReceivedEventHandler(this.StandardErrorReceiver);
                    //WriteToStandardInput(debuggedProcess);
                    // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.datareceivedeventhandler?view=netframework-4.8
                    // https://stackoverflow.com/questions/7850352/how-can-i-copy-the-stdout-of-a-process-copy-not-redirect

                    // TODO: set duplicate http://vsokovikov.narod.ru/New_MSDN_API/Handles_objects/fn_duplicatehandle.htm
                    // http://vsokovikov.narod.ru/New_MSDN_API/Process_thread/child_process_redirect_io.htm
                    
                    this.debuggedProcess.Start();

                    this.debuggedProcess.BeginOutputReadLine();
                    this.debuggedProcess.BeginErrorReadLine();

                    Output.Log("Started file: {0}" + this.debuggedProcess.StartInfo.FileName);
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
    }
}
