using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace c_sharp
{
    public class Redirection
    {
        private static NamedPipeClientStream pipeClient;

        public static void RedirectToPipe()
        {
            pipeClient = new NamedPipeClientStream(".", "VSConsoleOutputPipe", PipeDirection.Out);
            if (pipeClient != null)
            {
                Console.WriteLine("Please see console in Visual Studio output");
                pipeClient.Connect(50);
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
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                }
            }
        }
    }
}
