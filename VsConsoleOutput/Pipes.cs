using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsConsoleOutput
{
    class Pipes
    {
        public static void StartClient()
        {
            using (NamedPipeClientStream pipeClient =
                new NamedPipeClientStream(".", "testpipe", PipeDirection.In))
            {

                // Connect to the pipe or wait until the pipe is available.
                Output.Console("Attempting to connect to pipe...");
                pipeClient.Connect();

                Output.Console("Connected to pipe.");
                Output.Console("There are currently {0} pipe server instances open.",
                   pipeClient.NumberOfServerInstances);
                using (StreamReader sr = new StreamReader(pipeClient))
                {
                    // Display the read text to the console
                    string temp;
                    while ((temp = sr.ReadLine()) != null)
                    {
                        Output.Console("Received from server: {0}", temp);
                    }
                }
            }
            Output.Console("Press Enter to continue...");
        }
    }
}
