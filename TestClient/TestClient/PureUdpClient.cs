using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TestClient
{
    class PureUdpClient
    {
        public void Start()
        {
            UdpClient client = new UdpClient();
            while (true)
            {
                string content = Console.ReadLine();
                var bytes = Encoding.UTF8.GetBytes(content);
                client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234));
            }
            client.Close(); 
        }
    }
}
