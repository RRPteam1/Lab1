string hostname = "192.168.1.6";
int port = 3000;

var client = new Client.ClientCode.Client(hostname, port);
client.Start();
client.Run();