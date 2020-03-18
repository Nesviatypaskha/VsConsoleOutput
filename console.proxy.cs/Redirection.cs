using System;
using System.IO;
using System.IO.Pipes;

namespace proxy
{
    public class Redirection
    {
        private static NamedPipeClientStream s_Pipe;

        public static void Connect()
        {
            try
            {
                {
                    s_Pipe = new NamedPipeClientStream(".", "VsConsoleOutput", PipeDirection.Out);
                }
                if (s_Pipe != null)
                {
                    {
                        s_Pipe.Connect(3000);
                    }
                    if (s_Pipe.IsConnected)
                    {
                        var a_Context = new StreamWriter(s_Pipe);
                        if (a_Context != null)
                        {
                            Console.WriteLine("Console redirected to Output Window in Visual Studio...");
                            a_Context.AutoFlush = true;
                            Console.SetOut(a_Context);
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Console don't redirected because pipe not opened");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Console don't redirected because pipe not connected");
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: Console don't redirected because pipe not created");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}

static class Program
{
    static void Main(string[] args)
    {
        proxy.Redirection.Connect();
        System.Diagnostics.Trace.WriteLine("!!!!Startup function called!!!!");
    }
}