using System;
using System.IO;
using System.IO.Pipes;

namespace c_sharp
{
    public class Redirection
    {
        public static StreamWriter RedirectToPipe()
        {
            Console.WriteLine("DLL FROM EXTENTION...");
            StreamWriter sw = null;
            NamedPipeServerStream pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.Out);
            if (pipeServer != null)
            {
                Console.WriteLine("DLL FROM EXTENTION...");
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
            if (sw != null)
                sw.AutoFlush = true;
            return sw;
        }
    }
}
