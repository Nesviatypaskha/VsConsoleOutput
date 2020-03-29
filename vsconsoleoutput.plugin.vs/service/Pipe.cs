using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;

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

            private bool m_Running;
            private Thread m_RunningThread;
            private EventWaitHandle m_TerminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            private void ServerLoop()
            {
                while (m_Running)
                {
                    ProcessNextClient();
                }

                m_TerminateHandle.Set();
            }

            public void Start()
            {
                m_Running = true;
                m_RunningThread = new Thread(ServerLoop);
                m_RunningThread.Start();
            }

            public void Stop()
            {
                m_Running = false;
                m_TerminateHandle.WaitOne();
            }

            public void ProcessClientThread(object o)
            {
                var a_Context1 = (NamedPipeServerStream)o;
                while (a_Context1.IsConnected)
                {
                    var a_Context2 = new byte[CONSTANT.BUFFER_SIZE];
                    var a_Size = a_Context1.Read(a_Context2, 0, CONSTANT.BUFFER_SIZE);
                    if (a_Size != 0)
                    {
                        Output.WriteLine(Encoding.UTF8.GetString(a_Context2, 0, a_Size).Replace("\r", ""));
                    }
                }
                a_Context1.Close();
                a_Context1.Dispose();
            }

            public void ProcessNextClient()
            {
                try
                {
                    var a_Context1 = new NamedPipeServerStream(CONSTANT.PIPE_NAME, PipeDirection.InOut, 2);
                    a_Context1.WaitForConnection();
                    var a_Context2 = new Thread(ProcessClientThread);
                    a_Context2.Start(a_Context1);
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
