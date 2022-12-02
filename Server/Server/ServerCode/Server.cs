using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.ServerCode
{
    public class Server
    {
        private UdpClient udpClient;
        public readonly int PORT;

        //messages
        Thread netThread;
        private ConcurrentQueue<Network.Message> inMessages = new();
        private ConcurrentQueue<Tuple<Network.Packet, IPEndPoint>> outMessages = new();
        private ConcurrentQueue<GameObjects.Player> send_EndGame_packetTo = new();

        //game
        private ConcurrentDictionary<Field, byte> activeGames = new();
        private ConcurrentDictionary<IPEndPoint, Field> player_in_game = new();
        private Field game;

        private Utils.Locked<bool> run = new(false); //check if server is running
        public void Start() => run.var = true;
        public Server(int port)
        {
            PORT = port;
            udpClient = new UdpClient(PORT, AddressFamily.InterNetwork); //use ipv-4
        }

        /// <summary>
        /// Start shutdown
        /// </summary>
        public void Shutdown()
        {
            if (run.var)
            {
                Console.WriteLine("[Server] Shutdown initialised.");

                //close any active games
                Queue<Field> arenas = new Queue<Field>(activeGames.Keys);
                foreach (Field arena in arenas)
                    arena.Stop();

                run.var = false;
            }
        }
        public void Close()
        {
            netThread?.Join(TimeSpan.FromSeconds(10));
            udpClient.Close();
        }

        /// <summary>
        /// Add new game
        /// </summary>
        private void setUpNewGame()
        {
            game = new Field(this);
            game.Start();
            activeGames.TryAdd(game, 0);
        }

        /// <summary>
        /// Notifies server that game is over
        /// </summary>
        /// <param name="g">game that is notifying</param>
        public void EndGame_notify(Field g)
        {
            Field a;
            if (g.leftPlayer.isSet) player_in_game.TryRemove(g.leftPlayer.ip, out a);
            if (g.rightPlayer.isSet) player_in_game.TryRemove(g.rightPlayer.ip, out a);

            activeGames.TryRemove(g, out byte b); //remove from the active games
        }

        /// <summary>
        /// Main loop on server
        /// </summary>
        public void Run()
        {
            if (run.var)
            {
                Console.WriteLine($"[Server] is running on port: {PORT}");
                netThread = new Thread(new ThreadStart(netRun));
                netThread.Start();

                setUpNewGame(); //set up first game
            }

            bool temp = run.var; //temp val of state of server
            while (temp)
            {
                bool inQueueMessages = inMessages.TryDequeue(out Network.Message someMessage);
                if (inQueueMessages)
                {
                    if (someMessage.packet.type == Network.PacketType.RequestJoin)
                    {
                        bool add = game.TryAddPlayer(someMessage.sender, Encoding.ASCII.GetString(someMessage.packet.data)); //check it
                        if (add) player_in_game.TryAdd(someMessage.sender, game);

                        if (!add)
                        {
                            setUpNewGame();
                            game.TryAddPlayer(someMessage.sender, Encoding.ASCII.GetString(someMessage.packet.data));
                            player_in_game.TryAdd(someMessage.sender, game);
                        }
                        game.Enque(someMessage); //dispatch message
                    }
                    else
                    {
                        if (player_in_game.TryGetValue(someMessage.sender, out Field field))
                            field.Enque(someMessage);
                    }
                }
                else Thread.Sleep(1); //no messages

                temp &= run.var; //check for quit
            }
        }

        #region Net
        //reads and writes packets
        private void netRun()
        {
            if (!run.var) return;

            Console.WriteLine("[Server] Waiting for UDP datagrams on port {0}", PORT);

            while (run.var)
            {
                bool canRead = udpClient.Available > 0;
                int numToWrite = outMessages.Count;
                int numToDisconnect = send_EndGame_packetTo.Count;
                try
                {
                    if (canRead)
                    {
                        IPEndPoint ep = new(IPAddress.Any, 0);
                        byte[] data = udpClient.Receive(ref ep); //receive data

                        //enque a new message
                        Network.Message nm = new()
                        {
                            sender = ep,
                            packet = new Network.Packet(data),
                            recvTime = DateTime.Now
                        };

                        inMessages.Enqueue(nm);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
                //send queued messages
                for (int i = 0; i < numToWrite; i++)
                {
                    bool gotMessage = outMessages.TryDequeue(out Tuple<Network.Packet, IPEndPoint> msg);
                    if (gotMessage) msg.Item1.Send(udpClient, msg.Item2);                  
                }

                //notify of game end
                for (int i = 0; i < numToDisconnect; i++)
                {
                    bool gotMessage = send_EndGame_packetTo.TryDequeue(out GameObjects.Player to);
                    if (gotMessage)
                    {
                        Network.DB.CRU database = new();
                        var players = database.Read();
                        var newPlayer = new Network.DB.TOP() { name = to.Name, score = to.paddle.Score };

                        Network.DB.TOP found = new();
                        try
                        {
                            found = players.Where(x => x.name.Equals(newPlayer.name)).FirstOrDefault();
                        }
                        catch { found = null; }
                        if (found != null) database.Update(newPlayer.name, newPlayer.score);
                        else database.Create(newPlayer);
                        players = database.Read();

                        Network.Packet.EndGame eg = new();
                        eg.Array = Utils.Converter.BuildStr(players);
                        eg.Send(udpClient, to.ip);
                    }
                }

                if (!canRead && (numToWrite == 0) && (numToDisconnect == 0)) Thread.Sleep(1); //nothing is happening
            }

            Console.WriteLine("[Server] Done listening for UDP datagrams");

            Queue<Field> games = new(activeGames.Keys);
            if (games.Count > 0)
            {
                Console.WriteLine("[Server] Waiting for active games to finish...");
                foreach (Field arena in games)
                    arena.JoinThread();
            }

            //check who need to see end packet
            if (send_EndGame_packetTo.Count > 0)
            {
                Console.WriteLine("[Server] Notifying remaining clients of shutdown...");

                //send end game packet until got no one to send
                bool have = send_EndGame_packetTo.TryDequeue(out GameObjects.Player to);
                while (have)
                {
                    Network.Packet.EndGame bp = new();
                    bp.Send(udpClient, to.ip);
                    have = send_EndGame_packetTo.TryDequeue(out to);
                }
            }
        }

        /// <summary>
        /// Queues up a Packet to be send
        /// </summary>
        /// <param name="packet">packet</param>
        /// <param name="to">reciever of packet</param>
        public void SendPacket(Network.Packet packet, IPEndPoint to) => outMessages.Enqueue(new Tuple<Network.Packet, IPEndPoint>(packet, to));

        /// <summary>
        /// Queue a EndPacket
        /// </summary>
        /// <param name="to">reciver of packet</param>
        public void SendEnd(GameObjects.Player to) => send_EndGame_packetTo.Enqueue(to);
        #endregion
    }
}
