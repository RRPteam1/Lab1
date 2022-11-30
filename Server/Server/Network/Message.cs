using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network
{
    public class Message
    {
        public IPEndPoint sender { get; set; }
        public DateTime recvTime { get; set; }
        public Packet packet { get; set; }
    }
}
