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
            private static NamedPipeServerStream s_Pipe;
            private class CONSTANT
            {
                public const int BUFFER_SIZE = 256;
                public const string PIPE_NAME = "VsConsoleOutput";
            }

            bool running;
            Thread runningThread;
            EventWaitHandle terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            public string PipeName = "VsConsoleOutput";// { get; set; }

            void ServerLoop()
            {
                while (running)
                {
                    ProcessNextClient();
                }

                terminateHandle.Set();
            }

            public void Start()
            {
                running = true;
                runningThread = new Thread(ServerLoop);
                runningThread.Start();
            }

            public void Stop()
            {
                running = false;
                terminateHandle.WaitOne();
            }

            public virtual string ProcessRequest(string message)
            {
                return "";
            }

            public void ProcessClientThread(object o)
            {
                NamedPipeServerStream pipeStream = (NamedPipeServerStream)o;

                //TODO FOR YOU: Write code for handling pipe client here
                while (pipeStream.IsConnected)
                {
                    var a_Context1 = new byte[CONSTANT.BUFFER_SIZE];
                    var a_Size = pipeStream.Read(a_Context1, 0, CONSTANT.BUFFER_SIZE);
                    if (a_Size != 0)
                    {
                        Output.WriteLine(Encoding.UTF8.GetString(a_Context1, 0, a_Size).Replace("\r", ""));
                    }
                }

                pipeStream.Close();
                pipeStream.Dispose();
            }

            public void ProcessNextClient()
            {
                try
                {
                    NamedPipeServerStream pipeStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 2);
                    pipeStream.WaitForConnection();

                    //Spawn a new thread for each request and continue waiting
                    Thread t = new Thread(ProcessClientThread);
                    t.Start(pipeStream);
                }
                catch (Exception e)
                {//If there are no more avail connections (254 is in use already) then just keep looping until one is avail
                }
            }
        }
    }
}
