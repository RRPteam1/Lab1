using System;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int PORT = 6000;
            Server.Server server = new Server.Server(PORT);
            server.Start();
            server.Run();
            server.Close();

            Console.ReadKey(); //stop immediate closere
        }
    }
}
