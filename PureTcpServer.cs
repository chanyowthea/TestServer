using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    class PureTcpServer
    {
        public class ClientData1
        {
            public byte[] _Buffer;
        }

        TcpListener _TcpListener;
        Dictionary<TcpClient, ClientData1> _Clients = new Dictionary<TcpClient, ClientData1>();

        public ClientData1 GetClient(TcpClient client)
        {
            ClientData1 data = null;
            _Clients.TryGetValue(client, out data);
            return data;
        }

        public void AddClient(TcpClient client, ClientData1 data)
        {
            if (!_Clients.ContainsKey(client))
            {
                _Clients.Add(client, data);
            }
        }

        public void RemoveClient(TcpClient client)
        {
            if (_Clients.ContainsKey(client))
            {
                _Clients.Remove(client);
            }
        }

        public void Start()
        {
            _TcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 1234);
            _TcpListener.Start();
            _TcpListener.BeginAcceptTcpClient(AcceptCallback, _TcpListener);
        }

        public void Stop()
        {
            Dispose();
        }

        void AcceptCallback(IAsyncResult result)
        {
            var listener = result.AsyncState as TcpListener;
            var tcpClient = listener.EndAcceptTcpClient(result);
            Console.WriteLine("ReceiveBufferSize=" + tcpClient.ReceiveBufferSize);
            var data = new ClientData1 { _Buffer = new byte[tcpClient.ReceiveBufferSize] };
            AddClient(tcpClient, data);
            tcpClient.GetStream().BeginRead(data._Buffer, 0, data._Buffer.Length, ReceiveCallback, tcpClient);
            listener.BeginAcceptTcpClient(AcceptCallback, result.AsyncState);
        }

        void ReceiveCallback(IAsyncResult result)
        {
            var tcpClient = result.AsyncState as TcpClient;
            try
            {
                int receivedSize = tcpClient.GetStream().EndRead(result);
                if (receivedSize == 0)
                {
                    Console.WriteLine("client disconnect! tcpClient=" + tcpClient.Client.RemoteEndPoint);
                    RemoveClient(tcpClient);
                    return;
                }
                var data = GetClient(tcpClient);
                byte[] receivedBytes = new byte[receivedSize];
                Buffer.BlockCopy(data._Buffer, 0, receivedBytes, 0, receivedSize);
                Console.WriteLine("received data=" + Encoding.UTF8.GetString(receivedBytes));
                tcpClient.GetStream().BeginRead(data._Buffer, 0, data._Buffer.Length, ReceiveCallback, tcpClient);
            }
            catch (Exception ex)
            {
                RemoveClient(tcpClient);
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // collect garbage manually and do not execute finalize
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release 
        /// both managed and unmanaged resources; <c>false</c> 
        /// to release only unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _TcpListener.Stop();
                    if (_TcpListener != null)
                    {
                        _TcpListener = null;
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

}
