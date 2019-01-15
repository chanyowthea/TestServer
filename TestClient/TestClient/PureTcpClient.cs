using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TestClient
{
    class PureTcpClient
    {
        public void Start()
        {
            TcpClient client = new TcpClient();
            try
            {
                client.Connect("127.0.0.1", 1234);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey(); 
            }

            Console.WriteLine("send message: ");
            var str = Console.ReadLine();
            var stream = client.GetStream();
            var bytes = Encoding.UTF8.GetBytes(str);
            stream.Write(bytes, 0, bytes.Length);

            str = Console.ReadLine();
            bytes = Encoding.UTF8.GetBytes(str);
            stream.Write(bytes, 0, bytes.Length);

            Console.ReadKey(); 
            stream.Close(); 
            client.Close(); 
        }
    }
}
