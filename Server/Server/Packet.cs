using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace Server
{
    public enum PacketType : uint
    {
        AcceptJoin = 1,
        IsHereAck, //server acknowledges client`s state
        GameStart,
        GameState,
        GameEnd,
        //!server
        RequestJoin,
        IsHere,
        JoinAck,
        GameStartAck,
        //end of server packets
    }
    public class Packet
    {
        public byte[] data = new byte[0]; //data that is used as an answer to someone
        public long timestamp; //time when packet was created
        public PacketType type;

        public Packet(PacketType type)
        {
            this.type = type;
            timestamp = DateTime.Now.Ticks;
        }

        public Packet(byte[] vs)
        {
            int flag = 0; //beginning of byte array

            //type of packet
            type = (PacketType)BitConverter.ToUInt32(vs, 0);
            flag += sizeof(PacketType);

            //time of packet
            timestamp = BitConverter.ToInt64(vs, flag);
            flag += sizeof(long); //bz time.ticks is long type of value

            data = vs.Skip(flag).ToArray(); //data to load
        }

        /// <summary>
        /// Used to construct array for sending to a client
        /// </summary>
        /// <returns>byte[]</returns>
        public byte[] Construct()
        {
            int ptSize = sizeof(PacketType);
            int tsSize = sizeof(long);

            int i = 0;
            byte[] buf = new byte[ptSize + tsSize + data.Length];

            //type of packet
            BitConverter.GetBytes((uint)type).CopyTo(buf, i);
            i += ptSize;

            //time of packet
            BitConverter.GetBytes(timestamp).CopyTo(buf, i);
            i += tsSize;

            //data to load
            data.CopyTo(buf, i);
            _ = data.Length;

            return buf;
        }

        internal void Send(UdpClient udpClient, IPEndPoint endPoint)
        {
            if (data == null) return; //nothing to send
            byte[] info = Construct();
            udpClient.Send(info, info.Length, endPoint);
        }

    }

    #region packets Server
    /// <summary>
    /// See task https://github.com/RRPteam1/Lab1/issues/2
    /// </summary>
    public class AcceptJoin : Packet
    {
        public PaddleSide side
        {
            get { return (PaddleSide)BitConverter.ToUInt32(data, 0); }
            set { data = BitConverter.GetBytes((uint)value); }
        }

        public AcceptJoin() : base(PacketType.AcceptJoin) {
            //set player side or set it to the default state (None)
            data = new byte[sizeof(PaddleSide)];
            side = PaddleSide.None;
        }
        public AcceptJoin(byte[] data) : base(data) { }
    }

    public class IsHereAck : Packet
    {
        public IsHereAck() : base(PacketType.IsHereAck) { }
    }

    public class GameStart : Packet
    {
        public GameStart() : base(PacketType.GameStart) { }
    }

    //TODO game state
    #endregion
}
