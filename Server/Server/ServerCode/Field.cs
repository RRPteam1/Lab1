using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Net;

namespace Server.ServerCode
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
        public Utils.Locked<FieldState> State { get; private set; } = new();
        private GameObjects.Ball ball = new();
        public GameObjects.Player leftPlayer { get; private set; } = new();
        public GameObjects.Player rightPlayer { get; private set; } = new();
        private object setPlayerLock = new();
        private Stopwatch gameTimer = new();

        private Server server;
        private TimeSpan Timeout = TimeSpan.FromSeconds(20);
        private DateTime lastGameState;

        private ConcurrentQueue<Network.Message> messages = new();

        private Utils.Locked<bool> stopRequested = new(false);

        private Thread fieldThread;
        public readonly int Id;
        private static int nextId = 1;

        private Random random = new(); //random speed
        public Field(Server server)
        {
            this.server = server;
            Id = nextId++;
            State.var = FieldState.NoGame;

            leftPlayer.paddle = new(GameObjects.PaddleSide.Left);
            rightPlayer.paddle = new(GameObjects.PaddleSide.Right);
        }
        public bool TryAddPlayer(IPEndPoint playerIP, string nick)
        {
            if (State.var == FieldState.WaitingForPlayers)
            {
                lock (setPlayerLock)
                {
                    if (!leftPlayer.isSet)
                    {
                        leftPlayer.ip = playerIP;
                        leftPlayer.Name = nick;
                        return true;
                    }

                    if (!rightPlayer.isSet)
                    {
                        rightPlayer.ip = playerIP;
                        rightPlayer.Name = nick;
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
            Console.WriteLine("[{0:000}] Waiting for players", Id);
            TimeSpan gameTime = TimeSpan.Zero;

            TimeSpan notifyGameStartTimeout = TimeSpan.FromSeconds(2.5);
            TimeSpan sendGameStateTimeout = TimeSpan.FromMilliseconds(1000f / 30f);

            bool running = true;
            bool playerDropped = false;
            while (running)
            {
                Network.Message mes;
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

                        if (gotMessage && (mes.packet.type == Network.PacketType.GameStartAck))
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
                            Console.WriteLine("[{0:000}] Starting Game", Id);
                            gameTimer.Start();
                        }

                        break;

                    case FieldState.InGame:
                        TimeSpan now = gameTimer.Elapsed;
                        gameTime = now - gameTime;

                        if (gotMessage)
                        {
                            switch (mes.packet.type)
                            {
                                case Network.PacketType.PaddlePosition:
                                    handlePaddleUpdate(mes);
                                    break;

                                case Network.PacketType.IsHere:
                                    Network.Packet.IsHereAck hap = new();
                                    GameObjects.Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
                                    sendTo(player, hap);

                                    player.LastPacketReceivedTime = mes.recvTime;
                                    break;
                            }
                        }

                        ball.ServerSideUpdate(new DateTime(gameTime.Ticks));
                        checkForBallCollisions();

                        sendGameState(leftPlayer, sendGameStateTimeout);
                        sendGameState(rightPlayer, sendGameStateTimeout);
                        break;
                }

                if (gotMessage && (mes.packet.type == Network.PacketType.GameEnd))
                {
                    GameObjects.Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
                    running = false;
                    Console.WriteLine("[{0:000}] Quit detected from {1} at {2}", Id, player.paddle.Side, gameTimer.Elapsed);

                    if (player.paddle.Side == GameObjects.PaddleSide.Left)
                    {
                        if (rightPlayer.isSet)
                            server.SendEnd(rightPlayer);
                    }
                    else
                    {
                        if (leftPlayer.isSet)
                            server.SendEnd(leftPlayer);
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
            Console.WriteLine("[{0:000}] Game Over, total game time was {1}", Id, gameTimer.Elapsed);

            if (stopRequested.var)
            {
                Console.WriteLine("[{0:000}] Notifying Players of server shutdown", Id);

                if (leftPlayer.isSet)
                    server.SendEnd(leftPlayer);
                if (rightPlayer.isSet)
                    server.SendEnd(rightPlayer);
            }

            server.EndGame_notify(this);
        }
        public void JoinThread() => fieldThread.Join(100);
        public void Enque(Network.Message m) => messages.Enqueue(m);

        #region Net
        private void sendTo(GameObjects.Player player, Network.Packet packet)
        {
            server.SendPacket(packet, player.ip);
            player.LastPacketSentTime = DateTime.Now;
        }
        private bool timedOut(GameObjects.Player player)
        {
            if (player.LastPacketReceivedTime == DateTime.MinValue)
                return false;

            bool timeoutDetected = (DateTime.Now > (player.LastPacketReceivedTime.Add(Timeout)));
            if (timeoutDetected)
                Console.WriteLine("[{0:000}] Timeout detected on {1} player at {2}", Id, player.paddle.Side, gameTimer.Elapsed);

            return timeoutDetected;
        }
        private void Connection(GameObjects.Player player, Network.Message mes)
        {
            bool sentByPlayer = mes.sender.Equals(player.ip);
            if (sentByPlayer)
            {
                player.LastPacketReceivedTime = mes.recvTime;

                switch (mes.packet.type)
                {
                    case Network.PacketType.RequestJoin:
                        Console.WriteLine("[{0:000}] RequestJoin from {1} with nickname {2}", Id, player.ip, player.Name);
                        sendAcceptJoin(player);
                        break;

                    case Network.PacketType.JoinAck:
                        player.pickedSide = true;
                        break;

                    case Network.PacketType.IsHere:
                        Network.Packet.IsHereAck hap = new();
                        sendTo(player, hap);

                        if (!player.pickedSide)
                            sendAcceptJoin(player);

                        break;
                }
            }
        }
        public void sendAcceptJoin(GameObjects.Player player)
        {
            Network.Packet.AcceptJoin ajp = new()
            {
                Side = player.paddle.Side
            };
            sendTo(player, ajp);
        }
        private void GameStarting(GameObjects.Player player, TimeSpan retryTimeout)
        {
            if (player.ready) return;

            if (DateTime.Now >= (player.LastPacketSentTime.Add(retryTimeout)))
            {
                Network.Packet.GameStart gsp = new();
                gsp.Left = leftPlayer.Name;
                gsp.Right = rightPlayer.Name;
                sendTo(player, gsp);
            }
        }
        private void sendGameState(GameObjects.Player player, TimeSpan resendTimeout)
        {
            if (DateTime.Now >= (player.LastPacketSentTime.Add(resendTimeout)))
            {
                //set all objs
                Network.Packet.GameStatePacket gsp = new()
                {
                    LeftY = leftPlayer.paddle.Position.Y,
                    RightY = rightPlayer.paddle.Position.Y,
                    BallPosition = ball.Position,
                    LeftScore = leftPlayer.paddle.Score,
                    RightScore = rightPlayer.paddle.Score
                };
                lastGameState = DateTime.Now;
                sendTo(player, gsp);
            }
        }
        private void handlePaddleUpdate(Network.Message mes)
        {
            GameObjects.Player player = mes.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;

            if (mes.packet.timestamp > player.LastPacketReceivedTimestamp)
            {
                player.LastPacketReceivedTimestamp = mes.packet.timestamp;
                player.LastPacketReceivedTime = mes.recvTime;

                //set up paddle pos
                Network.Packet.PaddlePositionPacket ppp = new(mes.packet.ToBytesArr());
                player.paddle.Position.Y = ppp.Y;
            }
        }
        #endregion

        #region Collision
        private void checkForBallCollisions() //TODO: check comments here!
        {
            //top and bottom hitreg
            int ballY = ball.Position.Y;
            if ((ballY <= ball.TopmostY) || (ballY >= ball.BottommostY)) //if ((ballY <= 0) || (ballY + GameGeometry.BallSize.Y * 2 + 22 >= GameGeometry.PlayArea.Y))
            {
                ball.Speed.Y *= -1;
            }

            float ballX = ball.Position.X;
            if (ballX <= ball.LeftmostX) //if (ballX + GameGeometry.BallSize.X <= 0)
            {
                //right scored (reset ball)
                rightPlayer.paddle.Score++;
                Console.WriteLine("[{0:000}] Right Player scored ({1} vs {2}) at {3}", Id, leftPlayer.paddle.Score, rightPlayer.paddle.Score, gameTimer.Elapsed);
                ball.Initialize();
            }
            else if (ballX >= ball.RightmostX)
            {
                //left scored (reset ball)
                leftPlayer.paddle.Score++;
                Console.WriteLine("[{0:000}] Left Player scored ({1} vs {2}) at {3}", Id, leftPlayer.paddle.Score, rightPlayer.paddle.Score, gameTimer.Elapsed);
                ball.Initialize();
            }

            //ball collision with paddles
            GameObjects.PaddleCollision collision;
            if (leftPlayer.paddle.Collides(ball, out collision))
                processBallHitWithPaddle(collision);
            if (rightPlayer.paddle.Collides(ball, out collision))
                processBallHitWithPaddle(collision);
        }
        private void processBallHitWithPaddle(GameObjects.PaddleCollision collision)
        {
            //safety check
            if (collision == GameObjects.PaddleCollision.None) return;

            //go in the opposite direction
            ball.Speed.X *= -1;

            //hit reg with top or bottom
            if ((collision == GameObjects.PaddleCollision.WithTop) || (collision == GameObjects.PaddleCollision.WithBottom))
                ball.Speed.Y *= -1;
        }
        #endregion
    }
}
