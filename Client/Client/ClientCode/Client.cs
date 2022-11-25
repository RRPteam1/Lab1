using Client.GameObjects;
using Client.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Client.ClientCode
{
    public class Client : Game
    {
        //game
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        //net
        private UdpClient udpClient;
        public readonly string ServerHostname;
        public readonly int ServerPort;

        //time
        private DateTime lastPacketReceivedTime = DateTime.MinValue; //client Time
        private DateTime lastPacketSentTime = DateTime.MinValue; //client Time
        private long lastPacketReceivedTimestamp = 0; //server Time
        private TimeSpan IsHereTimeout = TimeSpan.FromSeconds(20);
        private TimeSpan sendPaddlePositionTimeout = TimeSpan.FromMilliseconds(1000f / 30f); //timeout

        //messages
        private Thread thread;
        private ConcurrentQueue<Message> inMessages = new ConcurrentQueue<Message>();
        private ConcurrentQueue<Packet> outMessages = new ConcurrentQueue<Packet>();

        //game objects
        private Ball ball;
        private Paddle left;
        private Paddle right;
        private Paddle ourPaddle;
        private float previousY;

        private Texture2D establishingConnectionMsg;
        private Texture2D waitingForGameStartMsg;
        private Texture2D gamveOverMsg;

        //states
        private ClientState state = ClientState.NotConnected;
        private Locked<bool> run = new Locked<bool>(false);
        private Locked<bool> send_end_game_pack = new Locked<bool>(false);

        public Client(string hostname, int port)
        {
            //content folder
            //var dir = new System.IO.FileInfo(AppDomain.CurrentDomain.BaseDirectory).Directory.Parent.Parent.Parent.FullName;
            //Content.RootDirectory = dir + "\\Content\\bin\\Windows\\Content";
            Content.RootDirectory = "Content";

            //graphics setup
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = Costants.CostantPlayField.X; //todo
            graphics.PreferredBackBufferHeight = Costants.CostantPlayField.Y; //todo
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();

            //game objects
            ball = new Ball();
            left = new Paddle(PaddleSide.Left);
            right = new Paddle(PaddleSide.Right);

            //connection
            ServerHostname = hostname;
            ServerPort = port;
            udpClient = new UdpClient(ServerHostname, ServerPort);
        }

        protected override void Initialize()
        {
            base.Initialize();
            left.Initialize();
            right.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(graphics.GraphicsDevice); //todo add sprites later

            //load the game objects
            ball.LoadContent(Content);
            left.LoadContent(Content);
            right.LoadContent(Content);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        protected override void UnloadContent()
        {
            thread?.Join(TimeSpan.FromSeconds(2));
            udpClient.Close();

            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState kbs = Keyboard.GetState(); //keyboard
            if (kbs.IsKeyDown(Keys.Escape)) //if player click esc to close game
            {
                if ((state == ClientState.Connecting) || (state == ClientState.WaitingForOtherPlayers) || (state == ClientState.InGame))
                    send_end_game_pack.var = true; //end game trigger


                //stop the net thread
                run.var = false;
                state = ClientState.GameOver;
                Exit();
            }

            //if player dissconected
            if (timedOut())
                state = ClientState.GameOver;

            bool gotMessage = inMessages.TryDequeue(out Message message);

            // Check for Bye From server
            if (gotMessage && (message.packet.type == PacketType.GameEnd))
            {
                //shutdown the network thread 
                run.var = false;
                state = ClientState.GameOver;
            }

            switch (state)
            {
                case ClientState.Connecting:
                    sendRequestJoin(TimeSpan.FromSeconds(1));
                    if (gotMessage)
                        handleConnectionSetupResponse(message.packet);
                    break;

                case ClientState.WaitingForOtherPlayers:
                    // Send pack IsHere
                    sendIsHere(TimeSpan.FromSeconds(0.2));
                    if (gotMessage)
                    {
                        switch (message.packet.type)
                        {
                            case PacketType.AcceptJoin:
                                sendAcceptJoinAck(); //mb didn`t recv do one more time
                                break;

                            case PacketType.IsHereAck:
                                //ack times record
                                lastPacketReceivedTime = message.recvTime;
                                if (message.packet.timestamp > lastPacketReceivedTimestamp)
                                    lastPacketReceivedTimestamp = message.packet.timestamp;
                                break;

                            case PacketType.GameStart:
                                //start the game and ack
                                sendGameStartAck();
                                state = ClientState.InGame;
                                break;
                        }

                    }
                    break;

                case ClientState.InGame:
                    sendIsHere(TimeSpan.FromSeconds(0.2)); //send IsHere

                    //update paddle
                    previousY = ourPaddle.Position.Y;
                    ourPaddle.ClientSideUpdate(gameTime);
                    sendPaddlePosition(sendPaddlePositionTimeout);
                    if (gotMessage)
                    {
                        switch (message.packet.type)
                        {
                            case PacketType.GameStart:
                                sendGameStartAck();
                                break;

                            case PacketType.IsHereAck:
                                lastPacketReceivedTime = message.recvTime;
                                if (message.packet.timestamp > lastPacketReceivedTimestamp)
                                    lastPacketReceivedTimestamp = message.packet.timestamp;
                                break;

                            case PacketType.GameState:
                                //update game state and make sure it is latest
                                if (message.packet.timestamp > lastPacketReceivedTimestamp)
                                {
                                    lastPacketReceivedTimestamp = message.packet.timestamp;

                                    GameStatePacket gsp = new GameStatePacket(message.packet.ToBytesArr());
                                    left.Score = gsp.LeftScore;
                                    right.Score = gsp.RightScore;
                                    ball.Position = gsp.BallPosition;

                                    // Update what's not our paddle
                                    if (ourPaddle.Side == PaddleSide.Left)
                                        right.Position.Y = gsp.RightY;
                                    else
                                        left.Position.Y = gsp.LeftY;

                                    //todo: GameState gsp = new GameState(message.packet.ToBytesArr());
                                    //set scores of players and ball pos
                                    //update paddles positions
                                }
                                break;
                        }
                    }
                    break;

                case ClientState.GameOver:
                    //its dead (lol)
                    break;
            }

            base.Update(gameTime);
        }

        public void Start()
        {
            run.var = true;
            state = ClientState.Connecting;
            thread = new Thread(new ThreadStart(netRun));
            thread.Start();
        }

        #region Graphic
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();
            switch (state)
            {
                case ClientState.Connecting:
                    //TODO: draw connecting!
                    Console.WriteLine("Pong -- Connecting to {0}:{1}", ServerHostname, ServerPort);
                    Window.Title = String.Format("Pong -- Connecting to {0}:{1}", ServerHostname, ServerPort);
                    break;

                case ClientState.WaitingForOtherPlayers:
                    //TODO: draw waiting players!
                    Console.WriteLine("Pong -- Waiting for 2nd Player");
                    Window.Title = String.Format("Pong -- Waiting for 2nd Player");
                    break;

                case ClientState.InGame:
                    ball.Draw(gameTime,spriteBatch);
                    left.Draw(gameTime,spriteBatch, Color.Red);
                    right.Draw(gameTime,spriteBatch, Color.Blue);
                    updateWindowTitleWithScore();
                    break;

                case ClientState.GameOver:
                    //TODO: draw game over and top 10
                    Console.WriteLine("Game Over!");
                    updateWindowTitleWithScore();
                    break;
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void drawCentered(Texture2D texture)
        {
            Vector2 textureCenter = new Vector2(texture.Width / 2, texture.Height / 2); //scale
            spriteBatch.Draw(texture, Costants.CostantScreenCenter, Rectangle.Empty, Color.White, 0, textureCenter, Vector2.One, SpriteEffects.None, 0); //mb layer should set to 1
        }
        private void updateWindowTitleWithScore()
        {
            string fmt = (ourPaddle.Side == PaddleSide.Left) ?
                "[{0}] -- Pong -- {1}" : "{0} -- Pong -- [{1}]";
            Window.Title = String.Format(fmt, left.Score, right.Score);
        }
        #endregion

        #region Net
        private void netRun()
        {
            while (run.var)
            {
                bool canRead = udpClient.Available > 0;
                int numToWrite = outMessages.Count;

                if (canRead)
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref ep); //recv

                    Message nm = new Message
                    {
                        sender = ep,
                        packet = new Packet(data),
                        recvTime = DateTime.Now
                    };

                    inMessages.Enqueue(nm);

                    Console.WriteLine("RCVD: {0}", nm.packet);
                }

                //write out queued
                for (int i = 0; i < numToWrite; i++)
                {
                    bool have = outMessages.TryDequeue(out Packet packet);
                    if (have)
                        packet.Send(udpClient);

                    Console.WriteLine("SENT: {0}", packet);
                }

                if (!canRead && (numToWrite == 0)) Thread.Sleep(1);
            }

            if (send_end_game_pack.var)
            {
                EndGame bp = new EndGame();
                bp.Send(udpClient);
                Thread.Sleep(1000);
            }
        }

        private void sendPacket(Packet packet)
        {
            outMessages.Enqueue(packet);
            lastPacketSentTime = DateTime.Now;
        }

        private void sendRequestJoin(TimeSpan retryTimeout)
        {
            //don`t spam them!
            if (DateTime.Now >= (lastPacketSentTime.Add(retryTimeout)))
            {
                RequestJoin gsp = new RequestJoin();
                sendPacket(gsp);
            }
        }

        private void sendAcceptJoinAck()
        {
            JoinAck ajap = new JoinAck();
            sendPacket(ajap);
        }

        private void handleConnectionSetupResponse(Packet packet)
        {
            if (packet.type == PacketType.AcceptJoin)
            {
                //check if old
                if (ourPaddle == null)
                {
                    AcceptJoin ajp = new AcceptJoin(packet.ToBytesArr());
                    if (ajp.Side == PaddleSide.Left)
                        ourPaddle = left;
                    else if (ajp.Side == PaddleSide.Right)
                        ourPaddle = right;
                    else
                        throw new Exception("Error, invalid paddle side given by server."); //lol how?
                }

                //resp send
                sendAcceptJoinAck();

                state = ClientState.WaitingForOtherPlayers; //new state of game
            }
        }

        private void sendIsHere(TimeSpan resendTimeout)
        {
            if (DateTime.Now >= (lastPacketSentTime.Add(resendTimeout)))
            {
                IsHere hp = new IsHere();
                sendPacket(hp);
            }
        }

        private void sendGameStartAck()
        {
            GameStartAckPacket gsap = new GameStartAckPacket();
            sendPacket(gsap);
        }

        private void sendPaddlePosition(TimeSpan resendTimeout)
        {
            //todo: anticheat here
            if (previousY == ourPaddle.Position.Y)
                return;

            if (DateTime.Now >= (lastPacketSentTime.Add(resendTimeout)))
            {
                PaddlePositionPacket ppp = new PaddlePositionPacket();
                ppp.Y = ourPaddle.Position.Y;

                sendPacket(ppp); //TODO: paddle pos new send
            }
        }
        private bool timedOut()
        {
            //new record
            if (lastPacketReceivedTime == DateTime.MinValue)
                return false;
            return (DateTime.Now > (lastPacketReceivedTime.Add(IsHereTimeout)));
        }
        #endregion
    }
}