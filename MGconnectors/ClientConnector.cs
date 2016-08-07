using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MGconnectors
{
    public class ClientConnector
    {

        private TcpClient clientSocket = new TcpClient();
        private UdpClient broadcastingClient;
        private ConnectedDelegate receivingFunc;
        private bool needToStop = false;
        private SynchronizationContext context;

        public delegate void ConnectedDelegate(TcpClient socket);

        public ClientConnector()
        {
            try
            {
                broadcastingClient = new UdpClient(10406);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public bool isConnected()
        {
            return (clientSocket.Connected);
        }

        public void stopTrying()
        {
            needToStop = true;
        }

        public void startConnecting(ConnectedDelegate receivingFunc)
        {
            this.receivingFunc = receivingFunc;
            needToStop = false;
            string hostName = System.Net.Dns.GetHostName();

            System.Net.IPAddress broadcastIp = null;
            foreach (System.Net.IPAddress cur in System.Net.Dns.GetHostEntry(hostName).AddressList)
            {
                if ((cur.ToString()).StartsWith("192"))
                {
                    broadcastIp = cur;
                    break;
                }
            }
            try
            {
                string strIp = broadcastIp.ToString();
                broadcastIp = System.Net.IPAddress.Parse(strIp.Substring(0, strIp.LastIndexOf(".") + 1) + 255);
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }

            context = SynchronizationContext.Current;
            Task.Factory.StartNew( () => tryToReceiveIP(broadcastIp));
        }

        void tryToReceiveIP(System.Net.IPAddress broadcastIp)
        {
            
            while (!isConnected() && !needToStop)
            {
                broadcastingClient.Client.ReceiveTimeout = 5000;

                byte[] bytes = Encoding.ASCII.GetBytes("Hello");
                broadcastingClient.Send(bytes, bytes.Length, new System.Net.IPEndPoint(broadcastIp, 10408));

                System.Net.IPEndPoint ip = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 10406);

                try
                {
                    byte[] inBytes = broadcastingClient.Receive(ref ip);

                    if (inBytes != null)
                    {
                        System.Net.IPAddress serverIp = System.Net.IPAddress.Parse(Encoding.ASCII.GetString(inBytes));
                        connect(serverIp);
                    }
                }
                catch(SocketException e)
                {
                    System.Console.WriteLine("Не удалось подключиться");
                }
            }
        }

        private void connect(System.Net.IPAddress serverIp)
        {
            clientSocket.Connect(serverIp, 10407);
            if (isConnected()) context.Post((unused) => receivingFunc(clientSocket), new object());
        }
    }
}
