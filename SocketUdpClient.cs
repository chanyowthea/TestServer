using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace TestServer
{
    class SocketUdpClient
    {
        byte[] _Buffer = new byte[1024];

        public void Start()
        {
            Socket udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp); 
            udpServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234)); 
            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 1234); 
            while (true) 
            {
                int receiveSize = udpServer.ReceiveFrom(_Buffer, ref remoteIp);
                byte[] bytes = new byte[receiveSize]; 
                Buffer.BlockCopy(_Buffer, 0, bytes, 0, receiveSize);
                Console.WriteLine("received data=" + Encoding.UTF8.GetString(bytes));
                byte[] sendData = Encoding.UTF8.GetBytes("server has received your data! ");
                udpServer.SendTo(sendData, sendData.Length, SocketFlags.None, remoteIp);
            }
        }

    }

    /// <summary>
    /// UDP数据包分割器
    /// </summary>
    public class UdpPacketSplitter
    {
        public class UdpPacket
        {
            public UdpPacket(long sequence, int total, int i, byte[] chunk, int chunkLength)
            {

            }
        }

        /// <summary>
        /// 分割UDP数据包
        /// </summary>
        /// <param name="sequence">UDP数据包所持有的序号</param>
        /// <param name="datagram">被分割的UDP数据包</param>
        /// <param name="chunkLength">分割块的长度</param>
        /// <returns>
        /// 分割后的UDP数据包列表
        /// </returns>
        public static ICollection<UdpPacket> Split(long sequence, byte[] datagram, int chunkLength)
        {
            if (datagram == null)
                throw new ArgumentNullException("datagram");

            List<UdpPacket> packets = new List<UdpPacket>();

            // 整块的数量
            int chunks = datagram.Length / chunkLength;
            // 剩余数量
            int remainder = datagram.Length % chunkLength;
            int total = chunks;
            if (remainder > 0) total++;

            for (int i = 1; i <= chunks; i++)
            {
                byte[] chunk = new byte[chunkLength];
                Buffer.BlockCopy(datagram, (i - 1) * chunkLength, chunk, 0, chunkLength);
                // 包序列号，分包总个数，分包序号，分包数据，分包数据长度【分包数据长度可以不要】
                packets.Add(new UdpPacket(sequence, total, i, chunk, chunkLength));
            }
            if (remainder > 0)
            {
                int length = datagram.Length - (chunkLength * chunks);
                byte[] chunk = new byte[length];
                Buffer.BlockCopy(datagram, chunkLength * chunks, chunk, 0, length);
                packets.Add(new UdpPacket(sequence, total, total, chunk, length));
            }

            return packets;
        }

        //private void WorkThread()
        //{
        //    while (IsRunning)
        //    {
        //        waiter.WaitOne();
        //        waiter.Reset();

        //        while (queue.Count > 0)
        //        {
        //            StreamPacket packet = null;
        //            if (queue.TryDequeue(out packet))
        //            {
        //                RtpPacket rtpPacket = RtpPacket.FromImage(
        //                  RtpPayloadType.JPEG,
        //                  packet.SequenceNumber,
        //                  (long)Epoch.GetDateTimeTotalMillisecondsByYesterday(packet.Timestamp),
        //                  packet.Frame);

        //                // max UDP packet length limited to 65,535 bytes
        //                byte[] datagram = rtpPacket.ToArray();
        //                packet.Frame.Dispose();

        //                // split udp packet to many packets 
        //                // to reduce the size to 65507 limit by underlying IPv4 protocol
        //                ICollection<UdpPacket> udpPackets
        //                  = UdpPacketSplitter.Split(
        //                    packet.SequenceNumber,
        //                    datagram,
        //        -UdpPacket.HeaderSize);
        //                foreach (var udpPacket in udpPackets)
        //                {
        //                    byte[] udpPacketDatagram = udpPacket.ToArray();
        //                    // async sending
        //                    udpClient.BeginSend(
        //                      udpPacketDatagram, udpPacketDatagram.Length,
        //                      packet.Destination.Address,
        //                      packet.Destination.Port,
        //                      SendCompleted, udpClient);
        //                }
        //            }
        //        }
        //    }
        //}
    }
}
