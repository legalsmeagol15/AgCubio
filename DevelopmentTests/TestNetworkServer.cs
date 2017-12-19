using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using System.Net;
using Network_Controller;
using System.Threading;

namespace DevelopmentTests
{
    [TestClass]
    public class TestNetworkServer
    {
        [TestMethod]
        public void TestServer()
        {
            Network.Server_Awaiting_Client_Loop(ClientConnectedCallback, 11003);
            Network.ConnectToServer(ClientConnectionCallback, "localhost:11003");
        }

        private void ClientConnectionCallback(NetworkState state)
        {
            Assert.IsTrue(state.Socket.Connected);
        }

        protected void ClientConnectedCallback(NetworkState state)
        {
            Assert.IsTrue(state.Socket.Connected);
        }
    }
}
