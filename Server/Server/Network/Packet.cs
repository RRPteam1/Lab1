using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Network
{
    public class Packet
    {
        public byte[] data = Array.Empty<byte>();  //data that is used as an answer to someone
        public long timestamp; //time when packet was created
        public PacketType type;

        public override string ToString() => string.Format($"Packet={type}\ntimestamp={new DateTime(timestamp)}\ndata size={data.Length}");

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
            this.type = (PacketType)BitConverter.ToUInt32(vs, 0);
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

        #region Client packets
        public class RequestJoin : Packet
        {
            public string Nickname
            {
                get => BitConverter.ToString(data, 0);
                set => Encoding.ASCII.GetBytes(value).CopyTo(data, 0);
            }
            public RequestJoin() : base(PacketType.RequestJoin) => data = new byte[12]; //??I guess this ammount will be enough
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
            public GameObjects.PaddleSide Side
            {
                get { return (GameObjects.PaddleSide)BitConverter.ToUInt32(data, 0); }
                set { data = BitConverter.GetBytes((uint)value); }
            }

            public AcceptJoin() : base(PacketType.AcceptJoin)
            {
                data = new byte[sizeof(GameObjects.PaddleSide)];
                Side = GameObjects.PaddleSide.None; //default value
            }
            public AcceptJoin(byte[] bytes) : base(bytes) { }
        }

        public class IsHereAck : Packet
        {
            public IsHereAck() : base(PacketType.IsHereAck) { }
        }

        public class GameStart : Packet
        {
            private static readonly int leftIndex = 0;
            private static readonly int rightIndex = 12;
            public string Left
            {
                get { return BitConverter.ToString(data, leftIndex); }
                set { Encoding.ASCII.GetBytes(value).CopyTo(data, leftIndex); }
            }
            public string Right
            {
                get { return BitConverter.ToString(data, rightIndex); }
                set { Encoding.ASCII.GetBytes(value).CopyTo(data, rightIndex); }
            }
            public GameStart(byte[] bytes) : base(bytes) { }
            public GameStart() : base(PacketType.GameStart) {
                data = new byte[24]; //allocate data !we shouldn't hardcode this!
                //set defaults
                Left = string.Empty;
                Right = string.Empty; //608
            }
        }

        public class EndGame : Packet
        {
            private static readonly int ArrayIndex = 4;

            public string Array //array separator \n inline separator \t (exmp Alex\t3\n) => Name: Alex Score = 3
            {
                get { return BitConverter.ToString(data, ArrayIndex); }
                set { Encoding.ASCII.GetBytes(value).CopyTo(data, ArrayIndex); }
            }
            public EndGame() : base(PacketType.GameEnd) {
                data = new byte[164]; //160 bytes for array and 4 bytes to packet type
                Array = string.Empty; //empty array
            }
            public EndGame(byte[] bytes) : base(bytes) { }
        }

        public class PaddlePositionPacket : Packet
        {
            // The Paddle's Y position
            public int Y
            {
                get { return BitConverter.ToInt32(data, 0); }
                set { BitConverter.GetBytes(value).CopyTo(data, 0); }
            }
            public PaddlePositionPacket() : base(PacketType.PaddlePosition)
            {
                data = new byte[sizeof(Int32)];
                Y = 0; //default value is eq to zero
            }
            public PaddlePositionPacket(byte[] bytes) : base(bytes) { }

            public override string ToString() => $"Packet={this.type}\ntimestamp={new DateTime(timestamp)}\ndata size={data.Length}\nY={Y}";
        }

        public class GameStatePacket : Packet
        {
            //data offets
            private static readonly int leftYIndex = 0;
            private static readonly int rightYIndex = 4;
            private static readonly int ballPositionIndex = 8;
            private static readonly int leftScoreIndex = 16;
            private static readonly int rightScoreIndex = 20;

            //left Y position
            public int LeftY
            {
                get { return BitConverter.ToInt32(data, leftYIndex); }
                set { BitConverter.GetBytes(value).CopyTo(data, leftYIndex); }
            }

            //right Y position
            public int RightY
            {
                get { return BitConverter.ToInt32(data, rightYIndex); }
                set { BitConverter.GetBytes(value).CopyTo(data, rightYIndex); }
            }

            //ball position
            public Point BallPosition
            {
                get
                {
                    return new Point(
                        BitConverter.ToInt32(data, ballPositionIndex),
                        BitConverter.ToInt32(data, ballPositionIndex + sizeof(Int32))
                    );
                }
                set
                {
                    BitConverter.GetBytes(value.X).CopyTo(data, ballPositionIndex);
                    BitConverter.GetBytes(value.Y).CopyTo(data, ballPositionIndex + sizeof(Int32));
                }
            }

            //left score
            public int LeftScore
            {
                get { return BitConverter.ToInt32(data, leftScoreIndex); }
                set { BitConverter.GetBytes(value).CopyTo(data, leftScoreIndex); }
            }

            //right score
            public int RightScore
            {
                get { return BitConverter.ToInt32(data, rightScoreIndex); }
                set { BitConverter.GetBytes(value).CopyTo(data, rightScoreIndex); }
            }

            public GameStatePacket() : base(PacketType.GameState)
            {
                data = new byte[24]; //allocate data !we shouldn't hardcode this!

                //set defaults
                LeftY = 0;
                RightY = 0;
                BallPosition = new Point();
                LeftScore = 0;
                RightScore = 0;
            }

            public GameStatePacket(byte[] bytes) : base(bytes) { }

            public override string ToString()
            {
                return string.Format(
                    "Packet:{0}\ntimestamp={1}\ndata size={2}" +
                    "\nLeftY={3}" +
                    "\nRightY={4}" +
                    "\nBallPosition={5}" +
                    "\nLeftScore={6}" +
                    "\nRightScore={7}]",
                    this.type, new DateTime(timestamp), data.Length, LeftY, RightY, BallPosition, LeftScore, RightScore);
            }
        }
        #endregion
    }
}
