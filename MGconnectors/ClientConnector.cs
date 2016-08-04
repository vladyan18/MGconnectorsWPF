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
        private receivingDelegate receivingFunc;

        public delegate void receivingDelegate();

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

        public void startConnecting(receivingDelegate receivingFunc)
        {
            this.receivingFunc = receivingFunc;
            string hostName = System.Net.Dns.GetHostName();
            System.Net.IPAddress broadcastIp = System.Net.Dns.GetHostEntry(hostName).AddressList[2];
            string strIp = broadcastIp.ToString();
            broadcastIp = System.Net.IPAddress.Parse(strIp.Substring(0, strIp.LastIndexOf(".") + 1) + 255);
            new Thread(() => tryToReceiveIP(broadcastIp)).Start();
        }

        void tryToReceiveIP(System.Net.IPAddress broadcastIp)
        {
            while (!isConnected())
            {
                byte[] bytes = Encoding.ASCII.GetBytes("Hello");
                broadcastingClient.Send(bytes, bytes.Length, new System.Net.IPEndPoint(broadcastIp, 10408));
                broadcastingClient.BeginReceive(getServerIp, new object());
                Thread.Sleep(5000);
                if (!isConnected()) broadcastingClient.Close();
            }
        }

        private void getServerIp(IAsyncResult answer)
        {
            System.Net.IPEndPoint ip = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 10406);
            byte[] bytes = broadcastingClient.EndReceive(answer, ref ip);
            System.Net.IPAddress serverIp = System.Net.IPAddress.Parse(Encoding.ASCII.GetString(bytes));

            connect(serverIp);
        }

        private void connect(System.Net.IPAddress serverIp)
        {
            clientSocket.Connect(serverIp, 10407);
            if (isConnected()) receivingFunc.DynamicInvoke();
        }
    }
}
