using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using System.Net;
using Network_Controller;
using System.Threading;

namespace DevelopmentTests
{
    [TestClass]
    public class TestNetwork
    {
        private static System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
        public delegate void Receiver(IAsyncResult state);
        private Semaphore waitingForConnection;

        private string LONG_DATA {
            get
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader("..\\..\\..\\Resources\\SampleServerInput.txt"))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Opens a "server" socket, then uses the Network code to send to it
        /// </summary>
        [TestMethod]
        public void TestNetworkConnect()
        {
            waitingForConnection = new Semaphore(0, 1);
            int port = 11001;
            Socket server = StartNetworkListener(ReceiveTestNetworkConnect, port);
            Socket client = Network.ConnectToServer(ClientDataCallback, "localhost:" + port);
            /*waitingForConnection.WaitOne();
            Assert.IsTrue(server.Connected);
            Assert.IsTrue(client.Connected);*/
        }

        private void ClientDataCallback(NetworkState state)
        {
            Assert.IsTrue(state.Socket.Connected);
            if(state.ConnectionState == NetworkState.ConnectionStates.CONNECTED)
            {
                Network.Send(state.Socket, LONG_DATA);
            } else if(state.ConnectionState == NetworkState.ConnectionStates.HAS_DATA)
            {
                string data = encoding.GetString(state.Buffer);
                data = data.Replace("\0", "");
                Assert.AreEqual("Hello", data);
                state.ConnectionState = NetworkState.ConnectionStates.CONNECTED;
                state.CallBack = ClientMoreDataCallback;
                Network.RequestMoreData(state);
            }
        }

        private void ClientMoreDataCallback(NetworkState state)
        {
            string data = encoding.GetString(state.Buffer);
            data = data.Replace("\0", "");
            Assert.AreEqual("Goodbye", data);
        }

        private void ReceiveTestNetworkConnect(IAsyncResult state)
        {
            byte[] buffer = new byte[1024];
            Socket serverSocket = (Socket)state.AsyncState;
            Socket listeningSocket = serverSocket.EndAccept(state);
            listeningSocket.Receive(buffer, SocketFlags.None);
            Assert.IsTrue(listeningSocket.Connected);
            String data = encoding.GetString(buffer);
            data = data.Replace("\0", "");

            //The "server" doesn't have a way to check for having not recieved
            // complete packet, so just truncate and make sure we have the
            // right data, even if we don't have all of it
            Assert.AreEqual<string>(LONG_DATA.Substring(0, data.Length), data);
            listeningSocket.Send(encoding.GetBytes("Hello"));
            Thread.Sleep(10);
            listeningSocket.Send(encoding.GetBytes("Goodbye"));
        }

        private Socket StartNetworkListener(Receiver receiver, int port = 11000)
        {
            byte[] buffer = new byte[1024];

            IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, port);

            Socket serverSocket = new Socket(ipEndpoint.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(ipEndpoint);
            serverSocket.Listen(100);

            serverSocket.BeginAccept(new AsyncCallback(receiver), serverSocket);
            return serverSocket;
        }
    }
}
