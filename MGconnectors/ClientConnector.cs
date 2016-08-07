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
        private bool needToStop = false;

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

        public void stopTrying()
        {
            needToStop = true;
        }

        public void startConnecting(receivingDelegate receivingFunc)
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

            SynchronizationContext current = SynchronizationContext.Current;
            Task.Factory.StartNew( () => tryToReceiveIP(broadcastIp, current), new CancellationToken() );
        }

        void tryToReceiveIP(System.Net.IPAddress broadcastIp, SynchronizationContext context)
        {
            while (!isConnected() && !needToStop)
            {
                byte[] bytes = Encoding.ASCII.GetBytes("Hello");
                broadcastingClient.Send(bytes, bytes.Length, new System.Net.IPEndPoint(broadcastIp, 10408));
                broadcastingClient.BeginReceive(getServerIp, new object());
                Thread.Sleep(5000);
                if (!isConnected()) broadcastingClient.Close();
            }

            if (isConnected()) context.Post((unused) => receivingFunc(), new object());


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
        }
    }
}
