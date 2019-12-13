using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSConsoleOutputBeta
{
    class Pipes
    {
        private static NamedPipeServerStream pipeServer;
        
        public static void StartServer()
        {
            try
            {
                NamedPipeServerStream pipeServer = new NamedPipeServerStream("VSConsoleOutputBetaPipe", PipeDirection.In);
                pipeServer.WaitForConnection();
                using (StreamReader sr = new StreamReader(pipeServer))
                {
                    // Display the read text to the console
                    string temp;
                    while ((temp = sr.ReadLine()) != null)
                    {
                        OutputText.Write("Console", temp);
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
