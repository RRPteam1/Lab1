using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Lab1
{
    public partial class Form2 : Form
    {
        //net
        private readonly UdpClient udpClient;
        public readonly string? nickname; //TODO: use nickname
        public readonly string? ip;
        public readonly int port;

        //time
        private DateTime lastPacketReceivedTime = DateTime.MinValue; //client Time
        private DateTime lastPacketSentTime = DateTime.MinValue; //client Time
        private long lastPacketReceivedTimestamp = 0; //server Time
        private readonly TimeSpan IsHereTimeout = TimeSpan.FromSeconds(20);
        private readonly TimeSpan sendPaddlePositionTimeout = TimeSpan.FromMilliseconds(1000f / 40f); //timeout

        //messages
        private Thread? thread;
        private readonly ConcurrentQueue<Network.Message> inMessages = new();
        private readonly ConcurrentQueue<Network.Packet> outMessages = new();

        //game objects
        private readonly GameObjects.Ball ball;
        private readonly GameObjects.Paddle left;
        private readonly GameObjects.Paddle right;
        private GameObjects.Paddle? ourPaddle;

        //states
        private ClientState state = ClientState.NotConnected;
        private readonly Utils.Locked<bool> run = new(false);
        private readonly Utils.Locked<bool> send_end_game_pack = new(false);

        public Form2(string nickname, string ip, int port)
        {
            this.nickname = nickname; //send in join packet

            this.ip = ip;
            this.port = port;
            udpClient = new UdpClient(ip, port);

            InitializeComponent();
            left = new GameObjects.Paddle(Properties.Resources.red_Paddle, GameObjects.PaddleSide.Left, this);
            right = new GameObjects.Paddle(Properties.Resources.blue_Paddle, GameObjects.PaddleSide.Right, this);
            ball = new GameObjects.Ball(Properties.Resources.default_Ball, this);
            Start();
        }

        public void Start()
        {
            run.var = true;
            state = ClientState.Connecting;
            thread = new Thread(new ThreadStart(NetRun));
            thread.Start();
        }

        //TODO: checkout Draw metod!
        #region Graphic
        protected void Draw()
        {
            switch (state)
            {
                case ClientState.Connecting:
                    //TODO: draw connecting!
                    //Console.WriteLine("Pong -- Connecting to {0}:{1}", ip, port);
                    this.Text = String.Format("Pong -- Connecting to {0}:{1}", ip, port);
                    break;

                case ClientState.WaitingForOtherPlayers:
                    //TODO: draw waiting players!
                    //Console.WriteLine("Pong -- Waiting for 2nd Player");
                    this.Text = String.Format("Pong -- Waiting for 2nd Player");
                    break;

                case ClientState.InGame:
                    //TODO:Draw objects
                    //ball.Draw(gameTime, spriteBatch);
                    //left.Draw(gameTime, spriteBatch, Color.Red);
                    //right.Draw(gameTime, spriteBatch, Color.Blue);
                    UpdateWindowTitleWithScore();
                    break;

                case ClientState.GameOver:
                    //TODO: draw game over and top 10
                    //Console.WriteLine("Game Over!");
                    UpdateWindowTitleWithScore();
                    break;
            }
        }
        private void UpdateWindowTitleWithScore()
        {
            if (state == ClientState.GameOver)
            {
                Text = "Игра окончена";
            }
            else
            {
                string fmt = (ourPaddle?.Side == GameObjects.PaddleSide.Left) ? "[{0}] -- Pong -- {1}" : "{0} -- Pong -- [{1}]";
                this.Text = string.Format(fmt, left.Score, right.Score);
            }

        }
        #endregion

        #region Net
        private void NetRun()
        {
            do
            {
                bool canRead = udpClient.Available > 0;
                int numToWrite = outMessages.Count;

                if (canRead)
                {
                    try
                    {
                        IPEndPoint ep = new(IPAddress.Any, 0);
                        byte[] data = udpClient.Receive(ref ep); //recv

                        Network.Message nm = new()
                        {
                            sender = ep,
                            packet = new Network.Packet(data),
                            recvTime = DateTime.Now
                        };

                        inMessages.Enqueue(nm);
                    }
                    catch (Exception ex) { 
                        send_end_game_pack.var = true;
                        run.var = false;
                        state = ClientState.GameOver;
                    }
                }

                //write out queued
                for (int i = 0; i < numToWrite; i++)
                {
                    bool have = outMessages.TryDequeue(out Network.Packet? packet);
                    if (have)
                        packet?.Send(udpClient);
                }

                if (!canRead && (numToWrite == 0)) Thread.Sleep(1);
            } while (run.var);

            if (send_end_game_pack.var)
            {
                Network.Packet.EndGame bp = new();
                bp.Send(udpClient);
                Thread.Sleep(1000);
            }
        }

        private void SendPacket(Network.Packet packet)
        {
            outMessages.Enqueue(packet);
            lastPacketSentTime = DateTime.Now;
        }
        private void SendRequestJoin(TimeSpan retryTimeout)
        {
            //don`t spam them!
            if (DateTime.Now >= (lastPacketSentTime.Add(retryTimeout)))
            {
                Network.Packet.RequestJoin gsp = new();
                SendPacket(gsp);
            }
        }
        private void SendAcceptJoinAck()
        {
            Network.Packet.JoinAck ajap = new();
            SendPacket(ajap);
        }
        private void HandleConnectionSetupResponse(Network.Packet packet)
        {
            if (packet.type == Network.PacketType.AcceptJoin)
            {
                //check if old
                if (ourPaddle == null)
                {
                    Network.Packet.AcceptJoin ajp = new(packet.ToBytesArr());
                    if (ajp.Side == GameObjects.PaddleSide.Left)
                        ourPaddle = left;
                    else if (ajp.Side == GameObjects.PaddleSide.Right)
                        ourPaddle = right;
                    else
                        throw new Exception("Error, invalid paddle side given by server"); //lol how?
                }

                //resp send
                SendAcceptJoinAck();

                state = ClientState.WaitingForOtherPlayers; //new state of game
            }
        }
        private void SendIsHere(TimeSpan resendTimeout)
        {
            if (DateTime.Now >= (lastPacketSentTime.Add(resendTimeout)))
            {
                Network.Packet.IsHere hp = new();
                SendPacket(hp);
            }
        }
        private void SendGameStartAck()
        {
            Network.Packet.GameStartAckPacket gsap = new();
            SendPacket(gsap);
        }
        private void SendPaddlePosition(TimeSpan resendTimeout)
        {
            //todo: anticheat here
            //if (previousY == ourPaddle.Position.Y)
            //return;

            if (DateTime.Now >= (lastPacketSentTime.Add(resendTimeout)))
            {
                Network.Packet.PaddlePositionPacket ppp = new();
                ppp.Y = ourPaddle!.Position.Y;

                SendPacket(ppp); //TODO: paddle pos new send
            }
        }
        private bool TimedOut()
        {
            label4.Text = $"Last RCVD:{lastPacketReceivedTime}  RESULT = {(DateTime.Now > (lastPacketReceivedTime.Add(IsHereTimeout)))}";
            //new record
            if (lastPacketReceivedTime == DateTime.MinValue)
                return false;
            return (DateTime.Now > (lastPacketReceivedTime.Add(IsHereTimeout)));
        }
        #endregion

        /// <summary>
        /// If form is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            send_end_game_pack.var = true;
            run.var = false;
            state = ClientState.GameOver;
        }
        /// <summary>
        /// Game timer tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            label3.Text = $"State:{state}";
            //if player dissconected
            if (TimedOut()) state = ClientState.GameOver;
            bool gotMessage = inMessages.TryDequeue(out Network.Message? message);

            //check for game over from server
            if (gotMessage && (message!.packet.type == Network.PacketType.GameEnd))
            {
                //shutdown the network thread 
                run.var = false;
                state = ClientState.GameOver;
            }

            switch (state)
            {
                case ClientState.Connecting:
                    SendRequestJoin(TimeSpan.FromSeconds(1));
                    if (gotMessage)
                        HandleConnectionSetupResponse(message!.packet);
                    break;

                case ClientState.WaitingForOtherPlayers:
                    // Send pack IsHere
                    SendIsHere(TimeSpan.FromSeconds(0.2));
                    if (gotMessage)
                    {
                        switch (message?.packet.type)
                        {
                            case Network.PacketType.AcceptJoin:
                                SendAcceptJoinAck(); //mb didn`t recv do one more time
                                break;

                            case Network.PacketType.IsHereAck:
                                //ack times record
                                lastPacketReceivedTime = message.recvTime;
                                if (message.packet.timestamp > lastPacketReceivedTimestamp)
                                    lastPacketReceivedTimestamp = message.packet.timestamp;
                                break;

                            case Network.PacketType.GameStart:
                                //start the game and ack
                                SendGameStartAck();
                                state = ClientState.InGame;
                                break;
                        }

                    }
                    break;

                case ClientState.InGame:
                    SendIsHere(TimeSpan.FromSeconds(0.2)); //send IsHere
                    SendPaddlePosition(sendPaddlePositionTimeout);
                    if (gotMessage)
                    {
                        switch (message?.packet.type)
                        {
                            case Network.PacketType.GameStart:
                                SendGameStartAck();
                                break;

                            case Network.PacketType.IsHereAck:
                                lastPacketReceivedTime = message.recvTime;
                                if (message.packet.timestamp > lastPacketReceivedTimestamp)
                                    lastPacketReceivedTimestamp = message.packet.timestamp;
                                break;

                            case Network.PacketType.GameState:
                                //update game state and make sure it is latest
                                if (message.packet.timestamp > lastPacketReceivedTimestamp)
                                {
                                    lastPacketReceivedTime = message.recvTime;
                                    lastPacketReceivedTimestamp = message.packet.timestamp;

                                    Network.Packet.GameStatePacket gsp = new(message.packet.ToBytesArr());
                                    left.Score = gsp.LeftScore;
                                    right.Score = gsp.RightScore;
                                    ball.Position = gsp.BallPosition;

                                    //update paddles positions
                                    if (ourPaddle?.Side == GameObjects.PaddleSide.Left)
                                        right.Position = new Point(right.Position.X, gsp.RightY);
                                    else
                                        left.Position = new Point(left.Position.X, gsp.LeftY);
                                }
                                break;
                        }
                    }
                    break;

                case ClientState.GameOver:
                    //its dead (lol)
                    break;
            }
            Draw();
        }

        private void Form2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                //check if player wants quit
                if ((state == ClientState.Connecting) || (state == ClientState.WaitingForOtherPlayers) || (state == ClientState.InGame))
                    send_end_game_pack.var = true; //trigger to send end packet


                //stop the network thread
                run.var = false;
                state = ClientState.GameOver;
                this.Close();
            }

            //Up and Down keys events
            int locY = ourPaddle!.Position.Y;

            if (e.KeyCode == Keys.Up)
                locY -= Constants.ConstantPaddleSpeed;
            else if (e.KeyCode == Keys.Down)
                locY += Constants.ConstantPaddleSpeed;

            //bounds checking
            if (locY < 0) locY = 0;
            else if (locY + Constants.ConstantPaddleSize.Y > Constants.ConstantPlayField.Y)
                locY = Constants.ConstantPlayField.Y - Constants.ConstantPaddleSize.Y;
            ourPaddle.Position = new Point(ourPaddle.Position.X, locY);
        }
    }
}
