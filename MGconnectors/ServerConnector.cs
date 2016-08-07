using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MGconnectors
{
    public class ServerConnector
    {
        private TcpListener serverListener;
        private UdpClient broadcastingClient = new UdpClient(10408);
        private System.Net.IPAddress broadcastIp;
        private System.Net.IPAddress serverIp;
        private newClientDelegate callback;

        public delegate void newClientDelegate(TcpClient newClient);

        public ServerConnector()
        {
            try
            {
                broadcastingClient = new UdpClient(10408);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public void startListening(newClientDelegate callback)
        {
            this.callback = callback;

            string hostName = System.Net.Dns.GetHostName();
            serverIp = System.Net.Dns.GetHostEntry(hostName).AddressList[2];
            string strIp = serverIp.ToString();
            broadcastIp = System.Net.IPAddress.Parse(strIp.Substring(0, strIp.LastIndexOf(".") + 1) + 255);

            serverListener = new TcpListener(serverIp, 10407);
            serverListener.Start();
            serverListener.BeginAcceptTcpClient(acceptingClient, new object());

            broadcastingClient.BeginReceive(getHello, new Object());
        }

        void getHello(IAsyncResult hello)
        {
            System.Net.IPEndPoint ip = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 10408);
            byte[] bHello = broadcastingClient.EndReceive(hello, ref ip);
            if (Encoding.ASCII.GetString(bHello) == "Hello")
            {
                byte[] msg = Encoding.ASCII.GetBytes(serverIp.ToString());
                broadcastingClient.Send(msg, msg.Length, new System.Net.IPEndPoint(broadcastIp, 10406));
            }

            broadcastingClient.BeginReceive(getHello, new Object());
        }

        void acceptingClient(IAsyncResult result)
        {
            TcpClient newClient = serverListener.EndAcceptTcpClient(result);
            callback(newClient);
            serverListener.BeginAcceptTcpClient(acceptingClient, new object());
        }
    }
}
