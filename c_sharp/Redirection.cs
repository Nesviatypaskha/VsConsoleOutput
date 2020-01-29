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
            System.Diagnostics.Trace.WriteLine("Trace.VSConsoleOutputPipe");
            pipeClient = new NamedPipeClientStream(".", "VSConsoleOutputPipe", PipeDirection.Out);
            if (pipeClient != null)
            {
                Console.WriteLine("Please see console in Visual Studio output");
                pipeClient.Connect(10);
                if (pipeClient.IsConnected)
                {
                    try
                    {
                        StreamWriter sw = new StreamWriter(pipeClient);
                        if (sw != null)
                        {
                            sw.AutoFlush = true;
                            Console.SetOut(sw);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}
