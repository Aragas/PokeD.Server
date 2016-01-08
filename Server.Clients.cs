using System;
using System.Linq;

using Aragas.Core.Interfaces;

using PokeD.Core.Data.PokeD.Monster;

using PokeD.Server.Clients;


namespace PokeD.Server
{
    public partial class Server : IUpdatable, IDisposable
    {
        public void ClientConnected(IServerModule caller, IClient client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.OtherConnected(client);
        }
        public void ClientDisconnected(IServerModule caller, IClient client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.OtherDisconnected(client);
        }

        public void ClientServerMessage(IServerModule caller, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendServerMessage(message);
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
    }
}
