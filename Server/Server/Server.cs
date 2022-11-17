namespace Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    namespace Server
    {
        public class Server
        {
            private UdpClient udpClient;
            public readonly int PORT;

            public Server(int PORT)
            {
                this.PORT = PORT;
                udpClient = new UdpClient(PORT, AddressFamily.InterNetwork);
            }


            Thread netThread;
            private ConcurrentQueue<Message> inMessages = new ConcurrentQueue<Message>();
            private ConcurrentQueue<Tuple<Packet, IPEndPoint>> outMessages = new ConcurrentQueue<Tuple<Packet, IPEndPoint>>();
            private ConcurrentQueue<IPEndPoint> send_EndGame_pacetTo = new ConcurrentQueue<IPEndPoint>();

            private ConcurrentDictionary<Game, byte> activeGames  = new ConcurrentDictionary<Game, byte>();
            private ConcurrentDictionary<IPEndPoint, Game> player_in_game = new ConcurrentDictionary<IPEndPoint, Game>();
            private Game game;

            private Locked<bool> running = new Locked<bool>(); //check if server is running

            internal void Start() => running.var = true;

            public void Run()
            {
                if (running.var)
                {
                    Console.WriteLine($"Server is running on port: {PORT}!");
                    netThread = new Thread(() => netRun());
                    netThread.Start();
                    setUpNewGame();//set up first game
                }

                bool temp = running.var; //temp value of state of server
                while (temp)
                {
                    bool inQueueMessages = inMessages.TryDequeue(out Message someMessage);
                    if (inQueueMessages)
                    {
                        if (someMessage.packet.type == PacketType.RequestJoin)
                        {
                            bool add = game.TryAddPlayer(someMessage.sender);
                            if (add) player_in_game.TryAdd(someMessage.sender, game);

                            if (!add)
                            {
                                setUpNewGame();
                                game.TryAddPlayer(someMessage.sender);
                                player_in_game.TryAdd(someMessage.sender, game);
                            }
                            game.Enque(someMessage); //dispatch message
                        }
                        else if (player_in_game.TryGetValue(someMessage.sender, out Game game)) game.Enque(someMessage);
                    }
                    else Thread.Sleep(1);

                    temp &= running.var;
                }
            }

            private void setUpNewGame()
            {
                game = new Game(this);
                game.Start();
                activeGames.TryAdd(game, 0);
            }

            public void Close()
            {
                netThread?.Join(TimeSpan.FromSeconds(10));
                udpClient?.Close();
            }

            public void EndGame_notify(Game g)
            {
                Game endingGame;
                if (g.leftPlayer.isSet)
                    player_in_game.TryRemove(g.leftPlayer.ip, out endingGame);
                if (g.rightPlayer.isSet)
                    player_in_game.TryRemove(g.rightPlayer.ip, out endingGame);


                activeGames.TryRemove(g, out byte b); //remove from the active games
                Console.WriteLine($"TryRemove with data {b}");
            }
        }
    }
}
