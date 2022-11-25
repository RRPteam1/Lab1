using System;
using System.Net;

namespace Pong
{
    public class Message
    {
        public IPEndPoint sender { get; set; }
        public DateTime recvTime { get; set; }
        public Packet packet { get; set; }
    }
}