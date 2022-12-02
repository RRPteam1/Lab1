int port = 6000;
Server.ServerCode.Server server = new(port);
server.Start();
server.Run();
server.Close();
Console.ReadKey(); //wait to close

Console.CancelKeyPress += UserComboShutdown;
void UserComboShutdown(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    server?.Shutdown();
}