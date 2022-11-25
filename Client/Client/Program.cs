using Pong;

string hostname = "192.168.1.6";
int port = 3000;

var client = new Client(hostname, port);
client.Start();
client.Run();