using System;
using System.IO;
using System.IO.Pipes;

namespace proxy
{
    public class Redirection
    {
        private static NamedPipeClientStream s_Pipe = null;

        public static void Connect()
        {
            try
            {
                {
                    AppDomain.CurrentDomain.ProcessExit += new EventHandler(__OnExit);
                }
                {
                    s_Pipe = new NamedPipeClientStream(".", "VsConsoleOutput", PipeDirection.Out);
                    //s_Pipe = new NamedPipeClientStream(".", "VSConsoleOutputPipe_test", PipeDirection.Out);
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
                            a_Context.AutoFlush = true;
                            Console.WriteLine("Console redirected to Output Window in Visual Studio...");
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

        public static void Disconnect()
        {
            if (s_Pipe != null)
            {
                s_Pipe.Close();
                s_Pipe.Dispose();
                s_Pipe = null;
            }
        }

        private static void __OnExit(object sender, EventArgs e)
        {
            try
            {
                {
                    Disconnect();
                }
                if (s_Pipe != null)
                {
                    Console.WriteLine("Console redirection is stopped...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
            }
        }
    }
}
