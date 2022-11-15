using System;
using System.Net;

namespace Server
{
    public class Player
    {
        public Paddle paddle;
        public IPEndPoint ip;
        public DateTime LastPacketReceivedTime = DateTime.MinValue; //server time
        public DateTime LastPacketSentTime = DateTime.MinValue; //server time
        public long LastPacketReceivedTimestamp = 0; //client time
        public bool pickedSide = false;
        public bool Ready = false;
        public bool isSet { get => ip != null; }
    }
}
