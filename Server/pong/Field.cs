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
                                //TODO: case paddle pos

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
                    Console.WriteLine("[{0:000}] Quit detected from {1} at {2}",
                        Id, player.paddle.Side, gameTimer.Elapsed);

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
            Console.WriteLine("[{0:000}] Game Over, total game time was {1}", Id, gameTimer.Elapsed);

            if (stopRequested.var)
            {
                Console.WriteLine("[{0:000}] Notifying Players of server shutdown", Id);

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
                Console.WriteLine("[{0:000}] Timeout detected on {1} Player at {2}", Id, player.paddle.Side, gameTimer.Elapsed);

            return timeoutDetected;
        }

        private void Connection(Player player, Message message)
        {
            bool sentByPlayer = message.sender.Equals(player.ip);
            if (sentByPlayer)
            {
                player.LastPacketReceivedTime = message.recvTime;

                switch (message.packet.type)
                {
                    case PacketType.RequestJoin:
                        Console.WriteLine("[{0:000}] Join Request from {1}", Id, player.ip);
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
                //Todo: game state
                //set all objs
            }
        }

        private void handlePaddleUpdate(Message message)
        {
            Player player = message.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;

            if (message.packet.timestamp > player.LastPacketReceivedTimestamp)
            {
                player.LastPacketReceivedTimestamp = message.packet.timestamp;
                player.LastPacketReceivedTime = message.recvTime;

               //TODO: set up paddle pos
            }
        }
        #endregion

        #region Collision Methods
        private void checkForBallCollisions() { }

        private void processBallHitWithPaddle(PaddleSide collision) { }
        #endregion
    }
}

