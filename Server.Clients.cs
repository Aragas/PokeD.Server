using System;
using System.Linq;

using PokeD.Core.Data.PokeD.Monster;

using PokeD.Server.Clients;


namespace PokeD.Server
{
    public partial class Server
    {
        private static IClient ServerClient = new ServerClient();

        public void ClientConnected(IServerModule caller, IClient client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.OtherConnected(client);

            if(caller.ClientsVisible)
                Logger.Log(LogType.Server, $"The player {client.Name} joined the game from IP {client.IP}");
        }
        public void ClientDisconnected(IServerModule caller, IClient client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.OtherDisconnected(client);

            if (caller.ClientsVisible)
                Logger.Log(LogType.Server, $"The player {client.Name} disconnected, playtime was {DateTime.Now - client.ConnectionTime:HH\\:mm\\:ss}");
        }

        public void ClientServerMessage(IServerModule caller, IClient sender, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendServerMessage(sender, message);

            Logger.Log(LogType.Chat, message);
        }
        public void ClientPrivateMessage(IServerModule caller, IClient sender, IClient destClient, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendPrivateMessage(sender, destClient, message);
        }
        public void ClientGlobalMessage(IServerModule caller, IClient sender, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendGlobalMessage(sender, message);

            Logger.Log(LogType.Chat, $"<{sender.Name}>: {message}");
        }

        public void ClientTradeOffer(IServerModule caller, IClient client, Monster monster, IClient destClient)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeRequest(client, monster, destClient);
        }
        public void ClientTradeConfirm(IServerModule caller, IClient client, IClient destClient)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeConfirm(client, destClient);
        }
        public void ClientTradeCancel(IServerModule caller, IClient client, IClient destClient)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeCancel(client, destClient);
        }

        public void ClientPosition(IServerModule caller, IClient sender)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendPosition(sender);
        }
    }
}
