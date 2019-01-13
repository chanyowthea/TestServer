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
            MemoryStream writerStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(writerStream);
            writer.Write(10); 
            var writeBytes = Encoding.UTF8.GetBytes("Hello World! "); 
            writer.Write(writeBytes.Length); 
            writer.Write(writeBytes); 
            byte[] _Buffer = writerStream.ToArray(); 
            MemoryStream stream = new MemoryStream(_Buffer.Skip(4).ToArray());
            BinaryReader reader = new BinaryReader(stream);
            //Console.WriteLine(reader.ReadInt32());
            int contentLength = reader.ReadInt32();
            Console.WriteLine(contentLength);
            Console.WriteLine(reader.PeekChar() > 0);
            Console.WriteLine(Encoding.UTF8.GetString(reader.ReadBytes(contentLength)));
            Console.WriteLine(reader.PeekChar() > 0);
            Console.ReadKey();
            return;

            SocketTcpServer server = new SocketTcpServer();
            server.Start();
            // 防止运行后立即退出
            while (true) { }
        }
    }
}