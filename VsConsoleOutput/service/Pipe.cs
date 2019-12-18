using System;
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
                // TODO:
                //switch (type)
                //{
                //    case Type.INPUT:
                //        break;
                //    case Type.OUTPUT:
                //        break;
                //    case Type.ERROR:
                //        break;
                //}
                try
                {
                    var a_Context = new NamedPipeServerStream("VSConsoleOutputPipe", PipeDirection.In);
                    {
                        a_Context.WaitForConnection();
                    }
                    using (var a_Context1 = new StreamReader(a_Context))
                    {
                        // Display the read text to the console
                        var a_Context2 = "";
                        while ((a_Context2 = a_Context1.ReadLine()) != null)
                        {
                            Output.Write("Console", a_Context2);
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
