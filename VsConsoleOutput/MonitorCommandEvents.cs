using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsConsoleOutput
{
    public class MonitorCommandEvents
    {
        private EnvDTE.Events events;
        private EnvDTE.CommandEvents commandEvents;
        private EnvDTE80.Commands2 commands;
        public void Start(EnvDTE80.DTE2 DTE)
        {
            events = DTE.Events;
            commandEvents = events.get_CommandEvents(null, 0);
            commands = DTE.Commands as EnvDTE80.Commands2;
            commandEvents.BeforeExecute += OnBeforeExecute;

        }
        public void Close()
        {
            commandEvents.BeforeExecute -= OnBeforeExecute;
            commandEvents.AfterExecute -= OnAfterExecute;
        }
        private void OnBeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            string name = GetCommandName(Guid, ID);
            //Logger.Log("OnBeforeExecute GUID = {0}", Guid);
            //if (!string.IsNullOrEmpty(name))
            //    Logger.Log("OnBeforeExecute GUID = {0}", name);

            if (name == "Debug.Start")
            {
                Output.Log("OnBeforeExecute Debug.Start");
                //TODO test ivsdebugger4 to remove current and add previous
            }
            //else if (name != "")
            //{
            //    Output.Log("OnBeforeExecute = {0}", name);
            //}
        }
        private void OnAfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
        {
            string name = GetCommandName(Guid, ID);
            if (name == "Debug.Start")
                Output.Log("OnAfterExecute Debug.Start");
        }
        private string GetCommandName(string Guid, int ID)
        {
            if (Guid == null)
                return "null";
            try
            {
                return commands.Item(Guid, ID).Name;
            }
            catch (System.Exception)
            {
            }
            return "";
        }
    }
}
