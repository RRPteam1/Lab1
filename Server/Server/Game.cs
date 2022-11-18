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
            //todo
            throw new NotImplementedException();
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
            //todo
            throw new NotImplementedException();
        }

        private void Send(Player player, Packet packet)
        {
            server.SendPacket(packet, player.ip);
            player.LastPacketSentTime = DateTime.Now;
        }
        
        public void JoinThread() => gameThread.Join(100);
    }
}
