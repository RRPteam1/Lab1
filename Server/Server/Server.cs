namespace Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
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
                    netThread = new Thread(new ThreadStart(netRun));
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
            
            private void netRun()
            {
                if (!running.var)
                    return;

                Console.WriteLine($"Server is waiting for UDP datagrams on port {PORT}");

                while (running.var)
                {
                    bool canRead = udpClient.Available > 0;
                    int numToWrite = outMessages.Count;
                    int numToDisconnect = send_EndGame_pacetTo.Count;

                    // Get data if there is some
                    if (canRead)
                    {
                        Console.WriteLine("Got");
                        // Read in one datagram
                        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = udpClient.Receive(ref ep);              // Blocks

                        // Enque a new message
                        Message nm = new Message();
                        nm.sender = ep;
                        nm.packet = new Packet(data);
                        nm.recvTime = DateTime.Now;

                        inMessages.Enqueue(nm);

                        Console.WriteLine("RCVD: {0}", nm.packet);
                    }

                    // Write out queued
                    for (int i = 0; i < numToWrite; i++)
                    {
                        // Send some data
                        Tuple<Packet, IPEndPoint> msg;
                        bool have = outMessages.TryDequeue(out msg);
                        if (have)
                            msg.Item1.Send(udpClient, msg.Item2);

                        Console.WriteLine("SENT: {0}", msg.Item1);
                    }

                    // Notify clients of Bye
                    for (int i = 0; i < numToDisconnect; i++)
                    {
                        IPEndPoint to;
                        bool have = send_EndGame_pacetTo.TryDequeue(out to);
                        if (have)
                        {
                            EndGame bp = new EndGame();
                            bp.Send(udpClient, to);
                        }
                    }

                    // If Nothing happened, take a nap
                    if (!canRead && (numToWrite == 0) && (numToDisconnect == 0))
                        Thread.Sleep(1);
                }

                Console.WriteLine("Server done listening for UDP datagrams");

                // Wait for all game's thread to join
                Queue<Game> games = new Queue<Game>(activeGames.Keys);
                if (games.Count > 0)
                {
                    Console.WriteLine("Server is waiting for active games to finish...");
                    foreach (Game game in games)
                        game.JoinThread();
                }

                // See which clients are left to notify of Bye
                if (send_EndGame_pacetTo.Count > 0)
                {
                    Console.WriteLine("Server notifying remaining clients of shutdown...");

                    // run in a loop until we've told everyone else
                    bool have = send_EndGame_pacetTo.TryDequeue(out IPEndPoint to);
                    while (have)
                    {
                        EndGame bp = new EndGame();
                        bp.Send(udpClient, to);
                        have = send_EndGame_pacetTo.TryDequeue(out to);
                    }
                }
            }

            // Queues up a Packet to be send to another person
            public void SendPacket(Packet packet, IPEndPoint to)
            {
                outMessages.Enqueue(new Tuple<Packet, IPEndPoint>(packet, to));
            }

            // Will queue to send a EndGame to the specified endpoint
            public void SendEnd(IPEndPoint to)
            {
                send_EndGame_pacetTo.Enqueue(to);
            }
        }
    }
}
