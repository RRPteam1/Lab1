using System;
using System.Collections.Concurrent;
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
        //add ball
        //add timer to end the game

        //server setup
        private Server.Server server;
        private ConcurrentQueue<Message> messages = new ConcurrentQueue<Message>();
        public readonly int Id;
        private static int nextId = 1;
        private Thread gameThread;
        //end server setup


        public Locked<GameState> State { get; private set; } = new Locked<GameState>();
        public Player leftPlayer { get; private set; } = new Player();
        public Player rightPlayer { get; private set; } = new Player();
        private object setPlayerLock = new object(); //lock for thread safety

        public Game(Server.Server server)
        {
            this.server = server;
            Id = nextId++;
            State.var = GameState.NoGame;
            leftPlayer.paddle = new Paddle(PaddleSide.Left);
            rightPlayer.paddle = new Paddle(PaddleSide.Right);
        }

        private void Run()
        {
            Console.WriteLine($"room: {Id} waiting for players!");
            bool running = true;
            while (running)
            {
                bool gotMessage = messages.TryDequeue(out Message mes);

                switch (State.var)
                {
                    case GameState.WaitingForPlayers: 
                        if (gotMessage) { 
                            Connection(leftPlayer, mes);
                            Connection(rightPlayer, mes);

                            if (leftPlayer.Ready && rightPlayer.Ready) //ready or not
                            {
                                //try sending the GameStart packet immediately
                                GameStarting(leftPlayer, new TimeSpan());
                                GameStarting(rightPlayer, new TimeSpan());

                                State.var = GameState.GameStarting; //change game state
                            }
                        }
                        break;
                }
            }
        }
        
        private void GameStarting(object leftPlayer, TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }

        private void Connection(Player player, Message message)
        {
            bool messageFromPlayer = message.sender.Equals(player.ip);
            if (!messageFromPlayer) return;

            player.LastPacketReceivedTime = message.recvTime;
            switch (message.packet.type)
            {
                //clients packets to check
                //RequestJoin
                //AcceptJoinAck
                //IsHere
                default:  throw new NotImplementedException();
            }
            
        }

        public void JoinThread() => gameThread.Join(100);
    }
}
