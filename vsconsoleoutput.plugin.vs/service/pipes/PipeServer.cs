using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell;

namespace service.pipes
{
    public class PipePool : IPipeServer
    {
        private class CONSTANT
        {
            public const string PIPE_NAME = "VsConsoleOutput";
            public const int MAX_SERVER_INSTANCES = 10;
        }

        private readonly string m_pipeName;
        private readonly SynchronizationContext m_synchronizationContext;
        private readonly IDictionary<string, IPipeServer> m_servers; // ConcurrentDictionary is thread safe
        public PipePool(string name = "")
        {
            m_pipeName = String.IsNullOrEmpty(name) ? CONSTANT.PIPE_NAME : name;
            m_synchronizationContext = AsyncOperationManager.SynchronizationContext;
            m_servers = new ConcurrentDictionary<string, IPipeServer>();
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

        public string ServerId
        {
            get { return m_pipeName; }
        }

        public void Start()
        {
            StartNamedPipeServer();
        }

        public void Stop()
        {
            foreach (var server in m_servers.Values)
            {
                try
                {
                    UnregisterFromServerEvents(server);
                    server.Stop();
                }
                catch (Exception ex)
                {
                    service.Output.WriteError("Fialed to stop server");
                    service.Output.WriteError(ex.ToString());
                }
            }

            m_servers.Clear();
        }

        private void StartNamedPipeServer()
        {
            var Context = new Pipe(m_pipeName, CONSTANT.MAX_SERVER_INSTANCES);
            m_servers[Context.m_id] = Context;

            Context.ClientConnectedEvent += ClientConnectedHandler;
            Context.ClientDisconnectedEvent += ClientDisconnectedHandler;
            Context.MessageReceivedEvent += MessageReceivedHandler;

            Context.Start();
        }

        private void StopNamedPipeServer(string id)
        {
            UnregisterFromServerEvents(m_servers[id]);
            m_servers[id].Stop();
            m_servers.Remove(id);
        }

        private void UnregisterFromServerEvents(IPipeServer server)
        {
            server.ClientConnectedEvent -= ClientConnectedHandler;
            server.ClientDisconnectedEvent -= ClientDisconnectedHandler;
            server.MessageReceivedEvent -= MessageReceivedHandler;
        }

        private void OnMessageReceived(MessageReceivedEventArgs eventArgs)
        {
            m_synchronizationContext.Post(e => MessageReceivedEvent.SafeInvoke(this, (MessageReceivedEventArgs) e), eventArgs);
        }

        private void OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            m_synchronizationContext.Post(e => ClientConnectedEvent.SafeInvoke(this, (ClientConnectedEventArgs) e), eventArgs);
        }

        private void OnClientDisconnected(ClientDisconnectedEventArgs eventArgs)
        {
            m_synchronizationContext.Post(e => ClientDisconnectedEvent.SafeInvoke(this, (ClientDisconnectedEventArgs) e), eventArgs);
        }

        private void ClientConnectedHandler(object sender, ClientConnectedEventArgs eventArgs)
        {
            OnClientConnected(eventArgs);
            StartNamedPipeServer();
        }

        private void ClientDisconnectedHandler(object sender, ClientDisconnectedEventArgs eventArgs)
        {
            OnClientDisconnected(eventArgs);
            StopNamedPipeServer(eventArgs.ClientId);
        }

        private void MessageReceivedHandler(object sender, MessageReceivedEventArgs eventArgs)
        {
            OnMessageReceived(eventArgs);
        }

    }
}
