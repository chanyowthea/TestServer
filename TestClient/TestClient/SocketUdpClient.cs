using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TestClient
{
    class SocketUdpClient
    {
        // 分包发送
        byte[] _Buffer = new byte[1024];
        public void Start()
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234);
            client.SendTo(Encoding.UTF8.GetBytes("hello kitty! "), ip);
            while (true)
            {
                EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 1234);
                int receiveSize = client.ReceiveFrom(_Buffer, ref remoteIp);
                var receivedData = new byte[receiveSize];
                Buffer.BlockCopy(_Buffer, 0, receivedData, 0, receiveSize); 
                Console.WriteLine("recieved data=" + Encoding.UTF8.GetString(receivedData)); 
            }
        }
    }
}
