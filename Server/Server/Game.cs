using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;


namespace Server
{
    //game logic on server

    public enum GameState
    {
        NoGame,
        WaitingForPlayers,
        GameStarting,
        InGame,
        GameOver
    }
    public class Game
    {
        public Locked<GameState> State { get; private set; } = new Locked<GameState>();
        private Ball ball = new Ball();
        public Player leftPlayer { get; private set; } = new Player();      
        public Player rightPlayer { get; private set; } = new Player();
        private object setPlayerLock = new object();
        private Stopwatch gameTimer = new Stopwatch();

        //Setup server
        private Server.Server server;
        private TimeSpan timeout = TimeSpan.FromSeconds(20);

        //packets to queue
        private ConcurrentQueue<Message> messages = new ConcurrentQueue<Message>();

        //stop request
        private Locked<bool> stopRequest = new Locked<bool>(false); 

        //other
        private Thread gameThread;
        private Random random = new Random();
        public readonly int Id;
        private static int nextId = 1;

        public Game(Server.Server server)
        {
            this.server = server;
            Id = nextId++;
            State.var = GameState.NoGame;
            leftPlayer.paddle = new Paddle(PaddleSide.Left);
            rightPlayer.paddle = new Paddle(PaddleSide.Right);
        }
        
        public void Enque(Message m) => messages.Enqueue(m);
        public void Stop() => stopRequest.var = true;
        private void Run()
        {
            Console.WriteLine($"room: {Id} waiting for players!");
            bool running = true;

            TimeSpan notifyGameStartTimeout = TimeSpan.FromSeconds(2.5);
            TimeSpan sendGameStateTimeout = TimeSpan.FromMilliseconds(1000f / 30f); //how often to update the players

            while (running)
            {
                bool gotMessage = messages.TryDequeue(out Message mes);

                switch (State.var)
                {
                    case GameState.WaitingForPlayers: 
                        if (gotMessage) { 
                            Connection(leftPlayer, mes);
                            Connection(rightPlayer, mes);

                            if (leftPlayer.pickedSide && rightPlayer.pickedSide) //ready or not
                            {
                                //try sending the GameStart packet immediately
                                GameStarting(leftPlayer, new TimeSpan());
                                GameStarting(rightPlayer, new TimeSpan());

                                State.var = GameState.GameStarting; //change game state
                            }
                        }
                        break;
                    case GameState.GameStarting:
                        GameStarting(leftPlayer, notifyGameStartTimeout);
                        GameStarting(rightPlayer, notifyGameStartTimeout);
                        if(gotMessage && (mes.packet.type == PacketType.GameStartAck))
                        {
                            if (mes.sender.Equals(leftPlayer.ip)) leftPlayer.Ready = true;
                            else if(mes.sender.Equals(rightPlayer.ip)) rightPlayer.Ready = true;
                        }
                        if(leftPlayer.Ready && rightPlayer.Ready)
                        {
                            //init game objects
                            //send game states
                            State.var = GameState.InGame;
                            Console.WriteLine($"Game has started in the room {Id}!");
                            gameTimer.Start();
                        }
                        break;
                }
            }
        }

        public bool TryAddPlayer(IPEndPoint playerIP)
        {
            if (State.var == GameState.WaitingForPlayers)
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

            return false; //can`t add more
        }
        
        private void GameStarting(object leftPlayer, TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            State.var = GameState.WaitingForPlayers; //change state

            gameThread = new Thread(() => gameRun());
            gameThread.Start();
        }

        private void gameRun()
        {
            Console.WriteLine("[{0:000}] Waiting for players", Id);
            GameTime gameTime = new GameTime();

            // Varibables used in the switch
            TimeSpan notifyGameStartTimeout = TimeSpan.FromSeconds(2.5);
            TimeSpan sendGameStateTimeout = TimeSpan.FromMilliseconds(1000f / 30f);  // How often to update the players

            // The loop
            bool running = true;
            bool playerDropped = false;
            while (running)
            {
                // Pop off a message (if there is one)
                Message message;
                bool haveMsg = messages.TryDequeue(out message);

                switch (State.var)
                {
                    case GameState.WaitingForPlayers:
                        if (haveMsg)
                        {
                            // Wait until we have two players
                            Connection(leftPlayer, message);
                            Connection(rightPlayer, message);

                            // Check if we are ready or not
                            if (leftPlayer.pickedSide && rightPlayer.pickedSide)
                            {
                                // Try sending the GameStart packet immediately
                                _notifyGameStart(leftPlayer, new TimeSpan());
                                _notifyGameStart(rightPlayer, new TimeSpan());

                                // Shift the state
                                State.var = GameState.GameStarting;
                            }
                        }
                        break;

                    case GameState.GameStarting:
                        // Try sending the GameStart packet
                        _notifyGameStart(leftPlayer, notifyGameStartTimeout);
                        _notifyGameStart(rightPlayer, notifyGameStartTimeout);

                        // Check for ACK
                        if (haveMsg && (message.packet.type == PacketType.GameStartAck))
                        {
                            // Mark true for those who have sent something
                            if (message.sender.Equals(leftPlayer.ip))
                                leftPlayer.Ready = true;
                            else if (message.sender.Equals(rightPlayer.ip))
                                rightPlayer.Ready = true;
                        }

                        // Are we ready to send/received game data?
                        if (leftPlayer.Ready && rightPlayer.Ready)
                        {
                            // Initlize some game object positions
                            ball.Initialize();
                            leftPlayer.paddle.Initialize();
                            rightPlayer.paddle.Initialize();

                            // Send a basic game state
                            _sendGameState(leftPlayer, new TimeSpan());
                            _sendGameState(rightPlayer, new TimeSpan());

                            // Start the game timer
                            State.var = GameState.InGame;
                            Console.WriteLine("[{0:000}] Starting Game", Id);
                            gameTimer.Start();
                        }

                        break;

                    case GameState.InGame:
                        // Update the game timer
                        TimeSpan now = gameTimer.Elapsed;
                        gameTime = new GameTime(now, now - gameTime.TotalGameTime);

                        // Get paddle postions from clients
                        if (haveMsg)
                        {
                            switch (message.packet.type)
                            {
                                case PacketType.PaddlePos:
                                    _handlePaddleUpdate(message);
                                    break;

                                case PacketType.IsHere:
                                    // Respond with an ACK
                                    IsHerePacket hap = new IsHerePacket();
                                    Player player = message.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
                                    _sendTo(player, hap);

                                    // Record time
                                    player.LastPacketReceivedTime = message.recvTime;
                                    break;
                            }
                        }

                        //Update the game components
                        ball.ServerSideUpdate(GameTimer);
                        _checkForBallCollisions();

                        // Send the data
                        _sendGameState(leftPlayer, sendGameStateTimeout);
                        _sendGameState(rightPlayer, sendGameStateTimeout);
                        break;
                }

                // Check for a quit from one of the clients
                if (haveMsg && (message.packet.type == PacketType.GameEnd))
                {
                    // Well, someone dropped
                    Player player = message.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;
                    running = false;
                    Console.WriteLine("[{0:000}] Quit detected from {1} at {2}",
                        Id, player.paddle.Side, gameTimer.Elapsed);

                    // Tell the other one
                    if (player.paddle.Side == PaddleSide.Left)
                    {
                        // Left Quit, tell Right
                        if (rightPlayer.isSet)
                            server.SendEnd(rightPlayer.ip);
                    }
                    else
                    {
                        // Right Quit, tell Left
                        if (leftPlayer.isSet)
                            server.SendEnd(leftPlayer.ip);
                    }
                }

                // Check for timeouts
                playerDropped |= timeOut(leftPlayer);
                playerDropped |= timeOut(rightPlayer);

                // Small nap
                Thread.Sleep(1);

                // Check quit values
                running &= !stopRequest.var;
                running &= !playerDropped;
            }

            // End the game
            gameTimer.Stop();
            State.var = GameState.GameOver;
            Console.WriteLine("[{0:000}] Game Over, total game time was {1}", Id, gameTimer.Elapsed);

            // If the stop was requested, gracefully tell the players to quit
            if (stopRequest.var)
            {
                Console.WriteLine("[{0:000}] Notifying Players of server shutdown", Id);

                if (leftPlayer.isSet)
                    server.SendEnd(leftPlayer.ip);
                if (rightPlayer.isSet)
                    server.SendEnd(rightPlayer.ip);
            }

            // Tell the server that we're finished
            server.EndGame_notify(this);
        }

        private void _handlePaddleUpdate(Message message)
        {
            // Only two possible players
            Player player = message.sender.Equals(leftPlayer.ip) ? leftPlayer : rightPlayer;

            // Make sure we use the latest message **SENT BY THE CLIENT**  ignore it otherwise
            if (message.packet.timestamp > player.LastPacketReceivedTimestamp)
            {
                // record timestamp and time
                player.LastPacketReceivedTimestamp = message.packet.timestamp;
                player.LastPacketReceivedTime = message.recvTime;

                // "cast" the packet and set data
                //PaddlePositionPacket ppp = new PaddlePositionPacket(message.packet.GetBytes());
                //player.paddle.Position.Y = ppp.Y;
            }
        }

        private void _sendTo(Player player, Packet packet)
        {
            server.SendPacket(packet, player.ip);
            player.LastPacketSentTime = DateTime.Now;
        }

        private void _notifyGameStart(Player player, TimeSpan retryTimeout)
        {
            // check if they are ready already
            if (player.Ready)
                return;

            // Make sure not to spam them
            if (DateTime.Now >= (player.LastPacketSentTime.Add(retryTimeout)))
            {
                GameStart gsp = new GameStart();
                _sendTo(player, gsp);
            }
        }

        private void _sendGameState(Player player, TimeSpan resendTimeout)
        {
            if (DateTime.Now >= (player.LastPacketSentTime.Add(resendTimeout)))
            {
                // Set the data
                GameStatePacket gsp = new GameStatePacket();
                _sendTo(player, gsp);
            }
        }

        private void _checkForBallCollisions()
        {
        }

        private bool timeOut(Player player)
        {
            // We haven't recorded it yet
            if (player.LastPacketReceivedTime == DateTime.MinValue)
                return false;

            // Do math
            bool timeoutDetected = (DateTime.Now > (player.LastPacketReceivedTime.Add(timeout)));
            if (timeoutDetected)
                Console.WriteLine("[{0:000}] Timeout detected on {1} Player at {2}", Id, player.paddle.Side, gameTimer.Elapsed);

            return timeoutDetected;
        }

        private void Connection(Player player, Message message)
        {
            bool sentByPlayer = message.sender.Equals(player.ip); //check who send message
            if (sentByPlayer)
            {
                player.LastPacketReceivedTime = message.recvTime; //when we have heard them last time

                // Do they need their Side? or a heartbeat ACK
                switch (message.packet.type)
                {
                    case PacketType.RequestJoin:
                        Console.WriteLine($"In room {Id} Join Request from {player.ip}");
                        sendAcceptJoin(player);
                        break;

                    case PacketType.JoinAck: //they acknowledged
                        player.pickedSide = true;
                        break;

                    case PacketType.IsHere: //they are waiting for the game start, we need to respond with an ACK
                        IsHereAck hap = new IsHereAck();
                        Send(player, hap);
                        if (!player.pickedSide)
                            sendAcceptJoin(player);
                        break;
                }
            }
        }

        private void sendAcceptJoin(Player player)
        {
            AcceptJoin ajp = new AcceptJoin();
            ajp.side = player.paddle.Side;
            _sendTo(player, ajp);
        }

        private void Send(Player player, Packet packet)
        {
            server.SendPacket(packet, player.ip);
            player.LastPacketSentTime = DateTime.Now;
        }
        
        public void JoinThread() => gameThread.Join(100);
    }
}
