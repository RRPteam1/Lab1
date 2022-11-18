using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;


namespace Pong
{
    public enum PacketType : uint
    {
        AcceptJoin = 1,
        IsHereAck, //server acknowledges client`s state
        GameStart,
        GameState,
        GameEnd = 5,
        //!server
        RequestJoin = 6,
        IsHere,
        JoinAck,
        GameStartAck
    }

    public class Packet
    {
        public byte[] data = new byte[0];  //data that is used as an answer to someone
        public long timestamp; //time when packet was created
        public PacketType type;

        public override string ToString() => string.Format($"Packet={type}\t  timestamp={timestamp}\t  data size={data.Length}");

        #region Ctors
        /// <summary>
        /// Create Packet with empty data
        /// </summary>
        /// <param name="type">type of packet</param>
        public Packet(PacketType type)
        {
            this.type = type;
            timestamp = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Create a packet from a byte arr
        /// </summary>
        /// <param name="vs">array of bytes</param>
        public Packet(byte[] vs)
        {
            int flag = 0; //beginning of byte array

            //type of packet
            type = (PacketType)BitConverter.ToUInt32(vs, 0);
            flag += sizeof(PacketType);

            //time of packet
            timestamp = BitConverter.ToInt64(vs, flag);
            flag += sizeof(long); //bcz time.ticks is long type of value

            data = vs.Skip(flag).ToArray(); //data to load
        }
        #endregion

        /// <summary>
        /// Convert the packet to a byte arr
        /// </summary>
        /// <returns>new arr</returns>
        public byte[] ToBytesArr()
        {
            int ptSize = sizeof(PacketType);
            int tsSize = sizeof(long);

            int i = 0;
            byte[] buf = new byte[ptSize + tsSize + data.Length];

            //type of packet
            BitConverter.GetBytes((uint)this.type).CopyTo(buf, i);
            i += ptSize;

            //time of packet
            BitConverter.GetBytes(timestamp).CopyTo(buf, i);
            i += tsSize;

            //data to load
            data.CopyTo(buf, i);
            i += data.Length;

            return buf;
        }

        #region Send methods
        /// <summary>
        /// Send a packet to a set reciver
        /// </summary>
        /// <param name="udpClient">who is sender</param>
        /// <param name="endPoint">who is receiver</param>
        public void Send(UdpClient udpClient, IPEndPoint endPoint)
        {
            byte[] info = ToBytesArr();
            udpClient.Send(info, info.Length, endPoint);
        }

        /// <summary>
        /// Send a packet to the remote receiver
        /// </summary>
        /// <param name="client">who is sender</param>
        public void Send(UdpClient client)
        {
            byte[] bytes = ToBytesArr();
            client.Send(bytes, bytes.Length);
        }
        #endregion
    }

    #region Client packets
    public class RequestJoin : Packet
    {
        public RequestJoin() : base(PacketType.RequestJoin) { }
    }
    public class IsHere : Packet
    {
        public IsHere() : base(PacketType.IsHere) { }
    }
    public class JoinAck : Packet
    {
        public JoinAck() : base(PacketType.JoinAck) { }
    }
    public class GameStartAckPacket : Packet
    {
        public GameStartAckPacket() : base(PacketType.GameStartAck) { }
    }
    #endregion

    #region Server packets
    public class AcceptJoin : Packet
    {
        // Paddle side
        public PaddleSide Side
        {
            get { return (PaddleSide)BitConverter.ToUInt32(data, 0); }
            set { data = BitConverter.GetBytes((uint)value); }
        }

        public AcceptJoin() : base(PacketType.AcceptJoin)
        {
            data = new byte[sizeof(PaddleSide)];
            Side = PaddleSide.None; //default value
        }
        public AcceptJoin(byte[] bytes) : base(bytes) { }
    }

    public class IsHereAck : Packet
    {
        public IsHereAck() : base(PacketType.IsHereAck) { }
    }

    public class GameStart : Packet
    {
        public GameStart() : base(PacketType.GameStart) { }
    }
    #endregion

    public class EndGame : Packet
    {
        public EndGame() : base(PacketType.GameEnd) { }
    }
}