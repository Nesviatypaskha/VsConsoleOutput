﻿using System;
using System.IO;
using System.IO.Pipes;

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
                    using (var a_Context1 = new StreamReader(s_serverStream))
                    {
                        // Display the read text to the console
                        var a_Context2 = "";
                        while ((a_Context2 = a_Context1.ReadLine()) != null)
                        {
                            if (!m_connected)
                            {
                                m_connected = true;
                                Debug.Instance.m_attached = true;
                            }
                            else
                            {
                                Output.Write(Output.CONSOLE, a_Context2);
                            }
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
