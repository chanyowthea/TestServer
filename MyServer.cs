using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TestServer
{
    class SocketTcpServer1
    {
        class ClientData
        {
            public int _ReceivedDataLength;
        }

        Dictionary<Socket, ClientData> _Clients = new Dictionary<Socket, ClientData>();
        ClientData GetClient(Socket socket)
        {
            ClientData data = null;
            if (_Clients.TryGetValue(socket, out data))
            {
                return data;
            }
            return null;
        }

        const int _BaseSize = 1024;
        byte[] _Buffer = new byte[_BaseSize];
        int _CurBufferIndex;
        List<byte> _CurPacketBytes = new List<byte>();

        public void Start()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);
            serverSocket.Bind(endPoint);
            serverSocket.Listen(100);
            serverSocket.BeginAccept(AcceptCallback, serverSocket);

            // initialize
            _CurPacketBytes.Capacity = _BaseSize;
        }

        void AcceptCallback(IAsyncResult result)
        {
            Socket serverSocket = result.AsyncState as Socket;
            Socket clientSocket = serverSocket.EndAccept(result);
            _Clients.Add(clientSocket, new ClientData());
            clientSocket.BeginReceive(_Buffer, 0, _Buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
            serverSocket.BeginAccept(AcceptCallback, serverSocket);
        }

        void ReceiveCallback(IAsyncResult result)
        {
            var clientSocket = result.AsyncState as Socket;
            var clientData = GetClient(clientSocket);
            if (clientData == null)
            {
                clientSocket.EndReceive(result);
                clientSocket.BeginReceive(_Buffer, 0, _Buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
                return;
            }

            clientData._ReceivedDataLength = clientSocket.EndReceive(result);
            _CurBufferIndex = clientData._ReceivedDataLength - 1;
            MemoryStream stream = new MemoryStream(_Buffer.Skip(_CurBufferIndex + 1).ToArray());
            BinaryReader reader = new BinaryReader(stream);
            while (reader.BaseStream.Length > 0)
            {
                // 数据总长度[4个字节] + 版本号[2个字节] + 命令号[2个字节] + 消息内容长度[4个字节] + 不定长数据
                var totalLength = reader.ReadInt32();
                if (totalLength < 4)
                {
                    break;
                }
                var version = reader.ReadInt16();
                var commandId = reader.ReadInt16();
                var contentLength = reader.ReadInt32();
                var bytes = reader.ReadBytes(contentLength);

                // 当前的包不完整，等待后续数据
                if (bytes.Length < contentLength)
                {
                    _CurPacketBytes.AddRange(bytes);
                }
                _CurPacketBytes.Clear();
                _CurPacketBytes.AddRange(bytes);

                // process current packet
                Console.WriteLine(Encoding.UTF8.GetString(_CurPacketBytes.ToArray()));
            }
            clientSocket.BeginReceive(_Buffer, 0, _Buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
        }
    }
}
