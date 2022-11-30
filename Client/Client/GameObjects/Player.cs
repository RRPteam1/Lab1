using System;
using System.Net;

namespace Client.GameObjects
{

    public class Player
    {
        public Paddle paddle;
        public IPEndPoint ip;
        public DateTime LastPacketReceivedTime = DateTime.MinValue; //Server Time
        public DateTime LastPacketSentTime = DateTime.MinValue; //Server Time
        public long LastPacketReceivedTimestamp = 0; //Client Time
        public bool pickedSide = false;
        public bool ready = false;

        public bool isSet
        {
            get { return ip != null; }
        }
    }
}

