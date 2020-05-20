using System;
using System.IO.Pipes;
using System.Text;

namespace service.pipes
{
    internal class Pipe : IPipeServer
    {
        private readonly NamedPipeServerStream m_pipe;
        private bool m_isStopping;
        private readonly object m_lockingObject = new object();
        private const int BufferSize = 2048;
        public readonly string m_id;

        private class Info
        {
            public readonly byte[] m_buffer;
            public readonly StringBuilder m_stringBuilder;

            public Info()
            {
                m_buffer = new byte[BufferSize];
                m_stringBuilder = new StringBuilder();
            }
        }

        public Pipe(string pipeName, int maxNumberOfServerInstances)
        {
            m_pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            m_id = Guid.NewGuid().ToString();
        }

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
        public event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        public string ServerId
        {
            get { return m_id; }
        }


        public void Start()
        {
            try
            {
                m_pipe.BeginWaitForConnection(WaitForConnectionCallBack, null);
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
        }

        public void Stop()
        {
            m_isStopping = true;
            try
            {
                if (m_pipe.IsConnected)
                {
                    m_pipe.Disconnect();
                }
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
            finally
            {
                m_pipe.Close();
                m_pipe.Dispose();
            }
        }
        private void BeginRead(Info info)
        {
            try
            {
                m_pipe.BeginRead(info.m_buffer, 0, BufferSize, EndReadCallBack, info);
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
                throw;
            }
        }
        private void WaitForConnectionCallBack(IAsyncResult result)
        {
            if (!m_isStopping)
            {
                lock (m_lockingObject)
                {
                    if (!m_isStopping)
                    {
                        m_pipe.EndWaitForConnection(result);

                        OnConnected();

                        BeginRead(new Info());
                    }
                }
            }
        }
        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = m_pipe.EndRead(result);
            if (readBytes > 0)
            {
                var Context = (Info) result.AsyncState;

                // Get the read bytes and append them
                Context.m_stringBuilder.Append(Encoding.UTF8.GetString(Context.m_buffer, 0, readBytes));
                _ = Output.WriteLineAsync(Encoding.UTF8.GetString(Context.m_buffer, 0, readBytes).Replace("\r", ""));

                if (!m_pipe.IsMessageComplete) // Message is not complete, continue reading
                {
                    BeginRead(Context);
                }
                else // Message is completed
                {
                    // Finalize the received string and fire MessageReceivedEvent
                    var message = Context.m_stringBuilder.ToString().TrimEnd('\0');

                    OnMessageReceived(message);

                    // Begin a new reading operation
                    BeginRead(new Info());
                }
            }
            else // When no bytes were read, it can mean that the client have been disconnected
            {
                if (!m_isStopping)
                {
                    lock (m_lockingObject)
                    {
                        if (!m_isStopping)
                        {
                            OnDisconnected();
                            Stop();
                        }
                    }
                }
            }
        }
        private void OnMessageReceived(string message)
        {
            if (MessageReceivedEvent != null)
            {
                MessageReceivedEvent(this, new MessageReceivedEventArgs { Message = message });
            }
        }
        private void OnConnected()
        {
            if (ClientConnectedEvent != null)
            {
                ClientConnectedEvent(this, new ClientConnectedEventArgs { ClientId = m_id });
            }
        }
        private void OnDisconnected()
        {
            if (ClientDisconnectedEvent != null)
            {
                ClientDisconnectedEvent(this, new ClientDisconnectedEventArgs { ClientId = m_id });
            }
        }
    }
}
