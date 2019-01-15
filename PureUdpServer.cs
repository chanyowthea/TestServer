using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    class PureUdpServer
    {
        public void Start()
        {
            UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234));
            while (true)
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, 1234);
                var data = client.Receive(ref ip);
                string content = Encoding.UTF8.GetString(data);
                Console.WriteLine("content=" + content);
            }
            client.Close(); 
        }
    }
}
