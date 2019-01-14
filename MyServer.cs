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

        class PacketData
        {
            public int _Version;
            public int _CommandId;
            public int _PacketBytesTotalLength;
            public List<byte> _CurPacketBytes = new List<byte>();

            public PacketData()
            {
                _CurPacketBytes.Capacity = _BaseSize;
            }

            public int NeedBytes()
            {
                return _PacketBytesTotalLength - _CurPacketBytes.Count;
            }
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
        PacketData _CurPacketData;

        public void Start()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);
            serverSocket.Bind(endPoint);
            serverSocket.Listen(100);
            serverSocket.BeginAccept(AcceptCallback, serverSocket);
            Console.WriteLine("SocketTcpServer1 start! ");
        }

        void AcceptCallback(IAsyncResult result)
        {
            Socket serverSocket = result.AsyncState as Socket;
            Socket clientSocket = serverSocket.EndAccept(result);
            Console.WriteLine("SocketTcpServer1 accept client! clientSocket.RemoteEndPoint=" + clientSocket.RemoteEndPoint);
            _Clients.Add(clientSocket, new ClientData());
            try
            {
                clientSocket.BeginReceive(_Buffer, 0, _Buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
                serverSocket.BeginAccept(AcceptCallback, serverSocket);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        void ReceiveCallback(IAsyncResult result)
        {
            var clientSocket = result.AsyncState as Socket;
            var clientData = GetClient(clientSocket);
            try
            {
                if (clientData == null)
                {
                    clientSocket.EndReceive(result);
                    clientSocket.BeginReceive(_Buffer, 0, _Buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
                    return;
                }

                clientData._ReceivedDataLength = clientSocket.EndReceive(result);
                MemoryStream stream = new MemoryStream(_Buffer.Take(clientData._ReceivedDataLength).ToArray());
                BinaryReader reader = new BinaryReader(stream);
                while (reader.PeekChar() > 0)
                {
                    if (_CurPacketData != null)
                    {
                        if (_CurPacketData.NeedBytes() > 0)
                        {
                            Console.WriteLine("continue collect incomplete packet! _CurPacketData.NeedBytes()=" + _CurPacketData.NeedBytes());
                            _CurPacketData._CurPacketBytes.AddRange(reader.ReadBytes(_CurPacketData.NeedBytes()));
                        }

                        if (_CurPacketData.NeedBytes() == 0)
                        {
                            // process current packet
                            Console.WriteLine(Encoding.UTF8.GetString(_CurPacketData._CurPacketBytes.ToArray()));
                            _CurPacketData = null;
                        }
                        break;
                    }

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

                    // 初始化不完整的包, 当前的包不完整，等待后续数据
                    if (bytes.Length < contentLength)
                    {
                        _CurPacketData = new PacketData();
                        _CurPacketData._Version = version;
                        _CurPacketData._CommandId = commandId;
                        _CurPacketData._PacketBytesTotalLength = contentLength;
                        _CurPacketData._CurPacketBytes.AddRange(bytes);
                        Console.WriteLine("start collect incomplete packet! contentLength=" + contentLength + ", collect length=" + bytes.Length); 
                        break;
                    }

                    // process current packet
                    Console.WriteLine("receive packet data=" + Encoding.UTF8.GetString(bytes));
                }
                clientSocket.BeginReceive(_Buffer, 0, _Buffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
