using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TestClient
{
    class SocketTcpClient
    {
        Socket _ClientSocket; 
        MemoryStream _SendStream = new MemoryStream();
        BinaryWriter _SendWriter;

        public void Start()
        {
            _ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _SendWriter = new BinaryWriter(_SendStream); 
        }

        public void Connect()
        {
            try
            {
                _ClientSocket.Connect(IPAddress.Parse("127.0.0.1"), 1234);
                Console.WriteLine("connect to server successfully! ");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        int _Version = 1; 
        int _Command = 10086;
        public void SendMessage(string content)
        {
            _SendStream.Seek(0, SeekOrigin.Begin);
            var bytes = Encoding.UTF8.GetBytes(content);
            _SendWriter.Write(12 + bytes.Length);
            _SendWriter.Write((short)_Version++);
            _SendWriter.Write((short)_Command++);
            _SendWriter.Write(bytes.Length);
            _SendWriter.Write(bytes); 
            _ClientSocket.Send(_SendStream.ToArray()); 
        }
    }
}
