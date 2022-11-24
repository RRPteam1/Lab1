using System;
using System.Threading;
using System.Net;
using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;

namespace Pong
{
    public enum FieldState
    {
        NoGame,
        WaitingForPlayers,
        GameStarting,
        InGame,
        GameOver
    }

    public class Field
    {
        public Locked<FieldState> State { get; private set; } = new Locked<FieldState>();
        private Ball ball = new Ball();
        public Player leftPlayer { get; private set; } = new Player();
        public Player rightPlayer { get; private set; } = new Player();
        private object setPlayerLock = new object();
        private Stopwatch gameTimer = new Stopwatch();

        private Server server;
        private TimeSpan Timeout = TimeSpan.FromSeconds(20);

        private ConcurrentQueue<Message> messages = new ConcurrentQueue<Message>();

        private Locked<bool> stopRequested = new Locked<bool>(false);

        private Thread fieldThread;
        private Random random = new Random();
        public readonly int Id;
        private static int nextId = 1;

        public Field(Server server)
        {
            this.server = server;
            Id = nextId++;
            State.var = FieldState.NoGame;

            leftPlayer.paddle = new Paddle(PaddleSide.Left);
            rightPlayer.paddle = new Paddle(PaddleSide.Right);
        }

        public bool TryAddPlayer(IPEndPoint playerIP)
        {
            if (State.var == FieldState.WaitingForPlayers)
            {
                lock (setPlayerLock)
                {
                    if (!leftPlayer.isSet)
                    {
                        leftPlayer.ip = playerIP;
                        return true;
                    }

                    if (!rightPlayer.isSet)
                    {
                        rightPlayer.ip = playerIP;
                        return true;
                    }
                }
            }

            return false;
        }

        public void Start()
        {
            State.var = FieldState.WaitingForPlayers;

            fieldThread = new Thread(new ThreadStart(Run));
            fieldThread.Start();
        }

        public void Stop() => stopRequested.var = true;

        private void Run()
        {
            Console.WriteLine($"room: {Id} waiting for players!");
            GameTime gameTime = new GameTime();

            TimeSpan notifyGameStartTimeout = TimeSpan.FromSeconds(2.5);
            TimeSpan sendGameStateTimeout = TimeSpan.FromMilliseconds(1000f / 30f);

            bool running = true;
            bool playerDropped = false;
            while (running)
            {
                Message mes;
                bool gotMessage = messages.TryDequeue(out mes);

                switch (State.var)
                {
                    case FieldState.WaitingForPlayers:
                        if (gotMessage)
                        {
                            Connection(leftPlayer, mes);
                            Connection(rightPlayer, mes);

                            if (leftPlayer.pickedSide && rightPlayer.pickedSide)
                            {
                                GameStarting(leftPlayer, new TimeSpan());
                                GameStarting(rightPlayer, new TimeSpan());

                                State.var = FieldState.GameStarting;
                            }
                        }
                        break;

                    case FieldState.GameStarting:
                        GameStarting(leftPlayer, notifyGameStartTimeout);
                        GameStarting(rightPlayer, notifyGameStartTimeout);

                        if (gotMessage && (mes.packet.type == PacketType.GameStartAck))
                        {
                            if (mes.sender.Equals(leftPlayer.ip))
                                leftPlayer.ready = true;
                            else if (mes.sender.Equals(rightPlayer.ip))
                                rightPlayer.ready = true;
                        }

                        if (leftPlayer.ready && rightPlayer.ready)
                        {
                            ball.Initialize();
                            leftPlayer.paddle.Initialize();
                            rightPlayer.paddle.Initialize();

                            sendGameState(leftPlayer, new TimeSpan());
                            sendGameState(rightPlayer, new TimeSpan());

                            State.var = FieldState.InGame;
                            Console.WriteLine($"Game has started in the room {Id}!");
                            gameTimer.Start();
                        }

                        break;

                    case FieldState.InGame:
                        TimeSpan now = gameTimer.Elapsed;
                        gameTime = new GameTime(now, now - gameTime.TotalGameTime);

                        if (gotMessage)
                        {
                            switch (mes.packet.type)
                            {
                                //GET: case paddle pos
                                case PacketType.PaddlePosition:
                                    handlePaddleUpdate(mes);
                                    break;

                                case PacketType.IsHere:
                                    IsHereAck hap = new IsHereAck();
                                    Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
                                    sendTo(player, hap);

                                    player.LastPacketReceivedTime = mes.recvTime;
                                    break;
                            }
                        }

                        ball.ServerSideUpdate(gameTime);
                        checkForBallCollisions();

                        sendGameState(leftPlayer, sendGameStateTimeout);
                        sendGameState(rightPlayer, sendGameStateTimeout);
                        break;
                }

                if (gotMessage && (mes.packet.type == PacketType.GameEnd))
                {
                    Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
                    running = false;
                    Console.WriteLine($"[{Id}] Quit detected from {player.paddle.Side} at {gameTimer.Elapsed}");

                    if (player.paddle.Side == PaddleSide.Left)
                    {
                        if (rightPlayer.isSet)
                            server.SendEnd(rightPlayer.ip);
                    }
                    else
                    {
                        if (leftPlayer.isSet)
                            server.SendEnd(leftPlayer.ip);
                    }
                }

                playerDropped |= timedOut(leftPlayer);
                playerDropped |= timedOut(rightPlayer);

                Thread.Sleep(1);

                running &= !stopRequested.var;
                running &= !playerDropped;
            }

            gameTimer.Stop();
            State.var = FieldState.GameOver;
            Console.WriteLine($"[{Id}] Game Over, total game time was {gameTimer.Elapsed}");

            if (stopRequested.var)
            {
                Console.WriteLine($"[{Id}] Notifying Players of server shutdown");

                if (leftPlayer.isSet)
                    server.SendEnd(leftPlayer.ip);
                if (rightPlayer.isSet)
                    server.SendEnd(rightPlayer.ip);
            }

            server.EndGame_notify(this);
        }

        public void JoinThread()
        {
            fieldThread.Join(100);
        }

        public void Enque(Message m) => messages.Enqueue(m);

        #region Network Functions
        private void sendTo(Player player, Packet packet)
        {
            server.SendPacket(packet, player.ip);
            player.LastPacketSentTime = DateTime.Now;
        }

        private bool timedOut(Player player)
        {
            if (player.LastPacketReceivedTime == DateTime.MinValue)
                return false;

            bool timeoutDetected = (DateTime.Now > (player.LastPacketReceivedTime.Add(Timeout)));
            if (timeoutDetected)
                Console.WriteLine($"[{Id}] Timeout detected on {player.paddle.Side} Player at {gameTimer.Elapsed}");

            return timeoutDetected;
        }

        private void Connection(Player player, Message mes)
        {
            bool sentByPlayer = mes.sender.Equals(player.ip);
            if (sentByPlayer)
            {
                player.LastPacketReceivedTime = mes.recvTime;

                switch (mes.packet.type)
                {
                    case PacketType.RequestJoin:
                        Console.WriteLine($"[{Id}] Join Request from {player.ip}");
                        sendAcceptJoin(player);
                        break;

                    case PacketType.JoinAck:
                        player.pickedSide = true;
                        break;

                    case PacketType.IsHere:
                        IsHereAck hap = new IsHereAck();
                        sendTo(player, hap);

                        if (!player.pickedSide)
                            sendAcceptJoin(player);

                        break;
                }
            }
        }

        public void sendAcceptJoin(Player player)
        {
            AcceptJoin ajp = new AcceptJoin
            {
                Side = player.paddle.Side
            };
            sendTo(player, ajp);
        }

        private void GameStarting(Player player, TimeSpan retryTimeout)
        {
            if (player.ready)
                return;

            if (DateTime.Now >= (player.LastPacketSentTime.Add(retryTimeout)))
            {
                GameStart gsp = new GameStart();
                sendTo(player, gsp);
            }
        }

        private void sendGameState(Player player, TimeSpan resendTimeout)
        {
            if (DateTime.Now >= (player.LastPacketSentTime.Add(resendTimeout)))
            {
                //GET: game state
                //set all objs
                GameStatePacket gsp = new GameStatePacket
                {
                    LeftY = leftPlayer.paddle.Position.Y,
                    RightY = rightPlayer.paddle.Position.Y,
                    BallPosition = ball.Position,
                    LeftScore = leftPlayer.paddle.Score,
                    RightScore = rightPlayer.paddle.Score
                };

                sendTo(player, gsp);
            }
        }

        private void handlePaddleUpdate(Message mes)
        {
            Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;

            if (mes.packet.timestamp > player.LastPacketReceivedTimestamp)
            {
                player.LastPacketReceivedTimestamp = mes.packet.timestamp;
                player.LastPacketReceivedTime = mes.recvTime;

                //GET: set up paddle pos
                PaddlePositionPacket ppp = new PaddlePositionPacket(mes.packet.ToBytesArr());
                player.paddle.Position.Y = ppp.Y;
            }
        }
        #endregion

        #region Collision Methods
        private void checkForBallCollisions()
        {
            // Top/Bottom
            float ballY = ball.Position.Y;
            if ((ballY <= ball.TopmostY) || (ballY >= ball.BottommostY))
            {
                ball.Speed.Y *= -1;
            }

            // Ball left and right (the goals!)
            float ballX = ball.Position.X;
            if (ballX <= ball.LeftmostX)
            {
                // Right player scores! (reset ball)
                rightPlayer.paddle.Score += 1;
                Console.WriteLine("[{0:000}] Right Player scored ({1} -- {2}) at {3}",
                    Id, leftPlayer.paddle.Score, rightPlayer.paddle.Score, gameTimer.Elapsed);
                ball.Initialize();
            }
            else if (ballX >= ball.RightmostX)
            {
                // Left palyer scores! (reset ball)
                leftPlayer.paddle.Score += 1;
                Console.WriteLine("[{0:000}] Left Player scored ({1} -- {2}) at {3}",
                    Id, leftPlayer.paddle.Score, rightPlayer.paddle.Score, gameTimer.Elapsed);
                ball.Initialize();
            }

            // Ball with paddles
            PaddleCollision collision;
            if (leftPlayer.paddle.Collides(ball, out collision))
                processBallHitWithPaddle(collision);
            if (rightPlayer.paddle.Collides(ball, out collision))
                processBallHitWithPaddle(collision);
        }

        private void processBallHitWithPaddle(PaddleCollision collision)
        {
            // Safety check
            if (collision == PaddleCollision.None)
                return;

            // Increase the speed
            ball.Speed.X *= map((float)random.NextDouble(), 0, 1, 1, 1.25f);
            ball.Speed.Y *= map((float)random.NextDouble(), 0, 1, 1, 1.25f);

            // Shoot in the opposite direction
            ball.Speed.X *= -1;

            // Hit with top or bottom?
            if ((collision == PaddleCollision.WithTop) || (collision == PaddleCollision.WithBottom))
                ball.Speed.Y *= -1;
        }

        private float map(float x, float a, float b, float p, float q) => p + (x - a) * (q - p) / (b - a);

        #endregion

    }
}

