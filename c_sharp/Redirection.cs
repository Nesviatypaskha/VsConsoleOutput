using System;
using System.IO;
using System.IO.Pipes;

namespace c_sharp
{
    public class Redirection
    {
        private static NamedPipeClientStream pipeClient;

        public static void RedirectToPipe()
        {  
            try
            {
                pipeClient = new NamedPipeClientStream(".", "VSConsoleOutputPipe", PipeDirection.Out);
                if (pipeClient != null)
                {
                    pipeClient.Connect(500);
                    if (pipeClient.IsConnected)  
                    {
                        Console.WriteLine("Console redirected to Output Window in Visual Studio");
                        StreamWriter sw = new StreamWriter(pipeClient);
                        if (sw != null)
                        {
                            sw.AutoFlush = true;
                            Console.SetOut(sw);
                            Console.WriteLine("VSConsoleOutput - WORK FINE");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            ////https://stackoverflow.com/questions/45534741/how-to-set-the-output-handle-to-opened-console-in-a-windows-application
            ///
        }
    }
}
