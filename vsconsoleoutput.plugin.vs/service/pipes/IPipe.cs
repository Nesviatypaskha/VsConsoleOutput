using System;

namespace service.pipes
{
    public interface IPipeServer
    {
        string ServerId { get; }
        void Start();
        void Stop();
        event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
    }
    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }
    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
