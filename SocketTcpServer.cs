using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    class SocketTcpServer
    {
        /*
         * 客户端延时最低限制：100MS
         * Socket攻击分类：
         * 1.多个客户端连接服务端后无消息传输，当服务端连接数量满时，其余客户端无法连接。[即恶意连接]
         * 2.连接服务端后，大量发送无用数据或无延时发送数据导致服务端无法解析数据。[即发送非正常数据]
         * */
        
        private byte[] data = new byte[1024];

        public void Start()
        {
            // 实例化Socket对象，指定为TCP
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 设置网络终端地址和端口
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8966);
            // 绑定ipEndPoint
            serverSocket.Bind(ipEndPoint);
            // 服务端启动监听，最大客户端连接数为100
            serverSocket.Listen(100);
            // 开始异步接收客户端连接
            serverSocket.BeginAccept(AsyncAccept, serverSocket);
        }

        private void AsyncAccept(IAsyncResult result)
        {
            // 获取服务端Socket
            Socket serverSocket = (Socket)result.AsyncState;
            Socket clientSocket = serverSocket.EndAccept(result);
            // 添加客户端到SocketManager中
            ClientSocketManager.Instance.AddClient(clientSocket);
            // 开始异步接收客户端数据
            clientSocket.BeginReceive(data, 0, data.Length, SocketFlags.None, AsyncReceive, clientSocket);
            // 继续异步接收客户端连接，没有这句只能获取一个客户端
            serverSocket.BeginAccept(AsyncAccept, serverSocket);
        }

        private int num = 0;
        private int receive = 0;
        private int all = 0;

        private void AsyncReceive(IAsyncResult result)
        {
            // 获取客户端Socket
            Socket clientSocket = (Socket)result.AsyncState;
            // 获取客户端消息类
            ClientInfo clientInfo = ClientSocketManager.Instance.GetClientInfo(clientSocket);
            // 获取客户端消息长度
            // 返回值是收到的字节数
            clientInfo._length = clientSocket.EndReceive(result);
            // 判断客户端长度是否大于4
            // 获取头部信息，储存值为int类型，4字节
            // 可能会收到多个包，因此_length会减去包的长度
            // 如果是收到半个包该怎么接收呢？没有办法，不会执行
            if (clientInfo._length > 4)
            {
                // 服务端第几次客户端消息
                receive++;
                // 服务端解析某次客户消息的第几次缓存数据
                num = 0;
                // 获取缓存中数据的长度
                string dataStr = Encoding.UTF8.GetString(data).Substring(0, clientInfo._length);
                // 初始化某客户端的缓存数据
                // 为什么这里不直接用data的[0, clientInfo._length]
                clientInfo.surplusBuffer = Encoding.UTF8.GetBytes(dataStr);
                // 当某客户端的缓存数据大于4[即最基本的网络数据要带有消息长度模块，即4个字节]
                if (clientInfo.surplusBuffer.Length > 4)
                {
                    // 实例化readByteBuffer用于读取clientInfo.surplusBuffer缓存
                    ByteBuffer readByteBuffer = new ByteBuffer(clientInfo.surplusBuffer);
                    // 实例化writeByteBuffer用于写入解析后剩余的数据
                    ByteBuffer writeByteBuffer = new ByteBuffer();
                    clientInfo._dataLength = 0;
                    while (clientInfo._length >= clientInfo._dataLength)
                    {
                        try
                        {
                            clientInfo._dataLength = readByteBuffer.ReadInt();
                            // 判断某客户端缓存中数据长度是否大于（内容长度+10）

                            if (clientInfo._length >= clientInfo._dataLength)
                            {
                                num++;
                                // 读取版本号模块[2个字节]
                                clientInfo._visionId = readByteBuffer.ReadUShort();
                                // 读取命令号模块[2个字节]
                                clientInfo._commandId = readByteBuffer.ReadUShort();
                                // 读取内容长度[4个字节]
                                clientInfo._contentLength = readByteBuffer.ReadInt();
                                // 读取内容
                                string content = readByteBuffer.ReadString(clientInfo._contentLength);
                                all++;
                                // 可更换逻辑处理
                                Console.WriteLine(string.Format("==============第{0}次接收，解析第{1}个数据，第{2}个数据==============", receive, num, all));
                                Console.WriteLine("消息总长度：" + clientInfo._length);
                                Console.WriteLine("总内容：" + dataStr);
                                Console.WriteLine("消息长度：" + clientInfo._dataLength);
                                Console.WriteLine("版本号：" + clientInfo._visionId);
                                Console.WriteLine("命令号：" + clientInfo._commandId);
                                Console.WriteLine("内容长度：" + clientInfo._contentLength);
                                Console.WriteLine("内容：" + content);
                                clientInfo._length -= clientInfo._dataLength;
                                clientInfo._visionId = 0;
                                clientInfo._commandId = 0;
                                clientInfo._contentLength = 0;
                            }
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                }
            }
            // 再次异步接收数据，没有这句只能接收一次
            clientSocket.BeginReceive(data, 0, data.Length, SocketFlags.None, AsyncReceive, clientSocket);
        }
    }

    /// <summary>
    /// 客户端信息类
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// 剩余的字节数
        /// </summary>
        public byte[] surplusBuffer = null;
        /// <summary>
        /// 本次接收收到的字节数
        /// </summary>
        public int _length = 0;
        /// <summary>
        /// 正在解析的包的数据总长度
        /// </summary>
        public int _dataLength = 0;
        public ushort _visionId = 0;
        public ushort _commandId = 0;
        /// <summary>
        /// 正在解析的包的内容长度
        /// </summary>
        public int _contentLength = 0;
    }

    /// <summary>
    /// 客户端Socket管理类
    /// </summary>
    public class ClientSocketManager
    {
        private Dictionary<Socket, ClientInfo> dicClientSocket = new Dictionary<Socket, ClientInfo>();
        private static ClientSocketManager instance;
        public ClientSocketManager() { }
        public static ClientSocketManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ClientSocketManager();
                }
                return instance;
            }
        }

        public void AddClient(Socket clientSocket)
        {
            if (clientSocket != null && !dicClientSocket.ContainsKey(clientSocket))
            {
                ClientInfo clientInfo = new ClientInfo();
                dicClientSocket.Add(clientSocket, clientInfo);
            }
        }

        public void RemoveClient(Socket clientSocket)
        {
            if (clientSocket != null && dicClientSocket.ContainsKey(clientSocket))
            {
                dicClientSocket.Remove(clientSocket);
            }
        }

        public ClientInfo GetClientInfo(Socket clientSocket)
        {
            if (dicClientSocket.ContainsKey(clientSocket))
            {
                return dicClientSocket[clientSocket];
            }
            else
            {
                return null;
            }
        }
    }
}
