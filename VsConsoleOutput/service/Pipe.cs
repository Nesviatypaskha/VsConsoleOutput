using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace service
{
    namespace output
    {
        class Pipe
        {
            public static void StartServer(/*Type type*/)
            {
                try
                {
                    bool m_connected = false;
                    var s_serverStream = new NamedPipeServerStream("VSConsoleOutputPipe", PipeDirection.In);
                    {
                        s_serverStream.WaitForConnection();
                    }
                    const int BUFFERSIZE = 256;
                    while (s_serverStream.IsConnected)
                    {
                        byte[] buffer = new byte[BUFFERSIZE];
                        int nRead = s_serverStream.Read(buffer, 0, BUFFERSIZE);
                        if (nRead != 0)
                        {
                            m_connected = true;
                            string line = Encoding.UTF8.GetString(buffer, 0, nRead).Replace("\r", "");
                            Output.Write(Output.CONSOLE, line);
                        }                      
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
