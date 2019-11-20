using System;
using System.IO;
using System.IO.Pipes;

namespace c_sharp
{
    public class Redirection
    {
        public static StreamWriter RedirectToPipe()
        {
            FileStream fs = new FileStream("c:\\temp\\MyTest.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            NamedPipeServerStream pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.Out);
            if (pipeServer != null)
            {
                Console.Write("Waiting for client connection...");
                pipeServer.WaitForConnection();
                try
                {
                    sw = new StreamWriter(pipeServer);
                }
                catch (IOException e)
                {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }
            }
            sw.AutoFlush = true;
            return sw;
        }
    }
}
