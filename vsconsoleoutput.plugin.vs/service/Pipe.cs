using System;
using System.IO.Pipes;
using System.Text;

namespace service
{
    namespace output
    {
        class Pipe
        {
            private class CONSTANT
            {
                public const int BUFFER_SIZE = 256;
                public const string PIPE_NAME = "VsConsoleOutput";
            }

            public static void StartServer()
            {
                try
                {
                    var a_Context = new NamedPipeServerStream(CONSTANT.PIPE_NAME, PipeDirection.In);
                    {
                        a_Context.WaitForConnection();
                    }
                    while (a_Context.IsConnected)
                    {
                        var a_Context1 = new byte[CONSTANT.BUFFER_SIZE];
                        var a_Size = a_Context.Read(a_Context1, 0, CONSTANT.BUFFER_SIZE);
                        if (a_Size != 0)
                        {
                            Output.WriteLine(Encoding.UTF8.GetString(a_Context1, 0, a_Size).Replace("\r", ""));
                        }                      
                    }
                }
                catch (Exception ex)
                {
                    service.Output.WriteError(ex.ToString());
                }
            }
        }
    }
}
