using System.Net;

namespace Lab1.Network
{
    public class Message
    {
        public IPEndPoint sender { get; set; }
        public DateTime recvTime { get; set; }
        public Packet packet { get; set; }
    }
}
