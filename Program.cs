using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //SocketTcpServer1 server = new SocketTcpServer1();
            //PureTcpServer server = new PureTcpServer();

            PureUdpServer server = new PureUdpServer();
            server.Start();
            // 防止运行后立即退出
            while (true) { }
            //server.Stop(); 
        }
    }
}