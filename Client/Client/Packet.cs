using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Xna.Framework;

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
        GameStartAck,
        PaddlePosition //position of paddle
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

    public class EndGame : Packet
    {
        public EndGame() : base(PacketType.GameEnd) { }
    }

    public class PaddlePositionPacket : Packet
    {
        // The Paddle's Y position
        public float Y
        {
            get { return BitConverter.ToSingle(data, 0); }
            set { BitConverter.GetBytes(value).CopyTo(data, 0); }
        }

        public PaddlePositionPacket()
            : base(PacketType.PaddlePosition)
        {
            data = new byte[sizeof(float)];

            // Default value is zero
            Y = 0;
        }

        public PaddlePositionPacket(byte[] bytes)
            : base(bytes) { }

        public override string ToString()
        {
            return string.Format("[Packet:{0}\n  timestamp={1}\n  payload size={2}" +
                "\n  Y={3}]",
                this.type, new DateTime(timestamp), data.Length, Y);
        }
    }

    public class GameStatePacket : Packet
    {
        // Payload array offets
        private static readonly int _leftYIndex = 0;
        private static readonly int _rightYIndex = 4;
        private static readonly int _ballPositionIndex = 8;
        private static readonly int _leftScoreIndex = 16;
        private static readonly int _rightScoreIndex = 20;

        // The Left Paddle's Y position
        public float LeftY
        {
            get { return BitConverter.ToSingle(data, _leftYIndex); }
            set { BitConverter.GetBytes(value).CopyTo(data, _leftYIndex); }
        }

        // Right Paddle's Y Position
        public float RightY
        {
            get { return BitConverter.ToSingle(data, _rightYIndex); }
            set { BitConverter.GetBytes(value).CopyTo(data, _rightYIndex); }
        }

        // Ball position
        public Vector2 BallPosition
        {
            get
            {
                return new Vector2(
                    BitConverter.ToSingle(data, _ballPositionIndex),
                    BitConverter.ToSingle(data, _ballPositionIndex + sizeof(float))
                );
            }
            set
            {
                BitConverter.GetBytes(value.X).CopyTo(data, _ballPositionIndex);
                BitConverter.GetBytes(value.Y).CopyTo(data, _ballPositionIndex + sizeof(float));
            }
        }

        // Left Paddle's Score
        public int LeftScore
        {
            get { return BitConverter.ToInt32(data, _leftScoreIndex); }
            set { BitConverter.GetBytes(value).CopyTo(data, _leftScoreIndex); }
        }

        // Right Paddle's Score
        public int RightScore
        {
            get { return BitConverter.ToInt32(data, _rightScoreIndex); }
            set { BitConverter.GetBytes(value).CopyTo(data, _rightScoreIndex); }
        }

        public GameStatePacket()
            : base(PacketType.GameState)
        {
            // Allocate data for the payload (we really shouldn't hardcode this in...)
            data = new byte[24];

            // Set default data
            LeftY = 0;
            RightY = 0;
            BallPosition = new Vector2();
            LeftScore = 0;
            RightScore = 0;
        }

        public GameStatePacket(byte[] bytes)
            : base(bytes) { }

        public override string ToString()
        {
            return string.Format(
                "[Packet:{0}\n  timestamp={1}\n  payload size={2}" +
                "\n  LeftY={3}" +
                "\n  RightY={4}" +
                "\n  BallPosition={5}" +
                "\n  LeftScore={6}" +
                "\n  RightScore={7}]",
                this.type, new DateTime(timestamp), data.Length, LeftY, RightY, BallPosition, LeftScore, RightScore);
        }
    }
    #endregion


}