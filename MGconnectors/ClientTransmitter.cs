using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MGconnectors
{
    class ClientTransmitter
    {
        public delegate void ReceiveMessage(ClientTransmitter transm, string msg);
        public event ReceiveMessage receiveMessage;

        private ClientConnector connector;
        private TcpClient socket;
        private NetworkStream socketStream;
        private SynchronizationContext context;
        private bool closed = false;

        public ClientTransmitter()
        {
            connector = new ClientConnector();
            context = SynchronizationContext.Current;
        }

        // подключение в автоматическом режиме
        public void connect()
        {
            closed = false;
            connector.startConnecting((socket) => connected(socket));
        }

        // подключение в ручном режиме
        public void connect(string ip)
        {
            
        }

        public void close()
        {
            closed = true;
        }

        public void sendMessage(string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            socketStream.Write(buffer, 0, buffer.Length);
        }

        private void connected(TcpClient socket)
        {
            this.socket = socket;
            socketStream = socket.GetStream();
            beginRead();
        }

        private void beginRead()
        {
            while(!closed)
            {
                byte[] bMsg = new byte[1000];
                socketStream.Read(bMsg, 0, 1000);
                string msg = Encoding.UTF8.GetString(bMsg);
                context.Post((unused) => receiveMessage.Invoke(this, msg) , new Object());
            }
        }       
    }
}
