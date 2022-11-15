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

            private Locked<bool> running = new Locked<bool>(); //check if server is running

            internal void Start() => running.var = true;

            internal void Run()
            {
                if (running.var)
                {
                    Console.WriteLine($"Server is running on port: {PORT}!");
                    netThread = new Thread(() => netRun());
                    netThread.Start();

                    //set up first game
                }

                bool temp = running.var; //temp value of state of server
                while (temp)
                {
                    Message someMessage; bool inQueueMessages = inMessages.TryDequeue(out someMessage);
                    if (inQueueMessages)
                    {
                        throw new NotImplementedException(); //TODO: check type of data and process it
                    }
                }
            }

            internal void Close()
            {
                netThread?.Join(TimeSpan.FromSeconds(10));
                udpClient?.Close();
            }

            private void netRun() //TODO write and read date to or from the UdpClient
            {
                if (!running.var) return;
                Console.WriteLine($"Server is waiting for datagrams on port: {PORT}.");
                while (running.var)
                {
                    bool canRead = udpClient.Available > 0; //will use to check if we are able to read
                    int queuedOutgoing = outMessages.Count; //how many messages we have to write
                    int queuedToDisconnect = send_EndGame_pacetTo.Count; //how many messages of endGame

                    if (canRead)
                    {
                        IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] buffer = udpClient.Receive(ref iPEndPoint);

                        Message message = new Message
                        {
                            sender = iPEndPoint,
                            packet = new Packet(buffer), //new data
                            recvTime = DateTime.Now
                        }; //enqueued message

                        inMessages.Enqueue(message);
                        Console.WriteLine($"Server recieved: {message.packet}"); // show recievd new Data(buffer)
                    }

                    //send queued messages
                    for (int i = 0; i < queuedOutgoing; i++)
                    {
                        Tuple<Packet, IPEndPoint> tuple; //message to send
                        bool have = outMessages.TryDequeue(out tuple);

                        if (have)
                        {
                            tuple.Item1.Send(udpClient, tuple.Item2);

                            Console.WriteLine($"Server sent: {tuple.Item1}");
                        }
                    }

                }
                throw new NotImplementedException();
            }
        }
    }
}
