using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.PokeD.Monster;

using PokeD.Server.Clients;
using PokeD.Server.Data;


namespace PokeD.Server
{
    public partial class Server
    {
        private List<TradeInstance> CurrentTrades { get; } = new List<TradeInstance>(); 


        private static Client ServerClient { get; } = new ServerClient();

        public void ClientConnected(IServerModule caller, Client client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.OtherConnected(client);

            if(caller.ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} joined the game from IP {client.IP}");
        }
        public void ClientDisconnected(IServerModule caller, Client client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.OtherDisconnected(client);

            if (caller.ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} disconnected, playtime was {DateTime.Now - client.ConnectionTime:hh\\:mm\\:ss}");
        }

        public void ClientServerMessage(IServerModule caller, Client sender, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendServerMessage(sender, message);

            Logger.Log(LogType.Chat, message);
        }
        public void ClientPrivateMessage(IServerModule caller, Client sender, Client destClient, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendPrivateMessage(sender, destClient, message);
        }
        public void ClientGlobalMessage(IServerModule caller, Client sender, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendGlobalMessage(sender, message);

            Logger.Log(LogType.Chat, $"<{sender.Name}> {message}");
        }

        public void ClientTradeOffer(IServerModule caller, Client client, Monster monster, Client destClient)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeRequest(client, monster, destClient);
        }
        public void ClientTradeConfirm(IServerModule caller, Client client, Client destClient)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeConfirm(client, destClient);
        }
        public void ClientTradeCancel(IServerModule caller, Client client, Client destClient)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeCancel(client, destClient);
        }

        public void OnClientTradeOffer(Client client, Monster monster, Client destClient)
        {
            if (!CurrentTrades.Any(t => t.Equals(client.ID, destClient.ID)))
                CurrentTrades.Add(new TradeInstance { Player_0_ID = client.ID, Player_1_ID = destClient.ID });

            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(client.ID, destClient.ID));
            if (trade != null)
            {
                if(trade.Player_0_ID == client.ID)
                    trade.Player_0_Monster = monster;

                if (trade.Player_1_ID == client.ID)
                    trade.Player_1_Monster = monster;
            }
        }
        public void OnClientTradeConfirm(Client client, Client destClient)
        {
            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(client.ID, destClient.ID));
            if (trade != null)
            {
                if (trade.Player_0_ID == client.ID)
                    trade.Player_0_Confirmed = true;
                if (trade.Player_1_ID == client.ID)
                    trade.Player_1_Confirmed = true;

                if (trade.Player_0_Confirmed && trade.Player_1_Confirmed)
                {
                    DatabaseTradeSave(trade);
                    CurrentTrades.Remove(trade);
                }
            }
        }
        public void OnClientTradeCancel(Client client, Client destClient)
        {
            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(client.ID, destClient.ID));
            if (trade != null)
                CurrentTrades.Remove(trade);
        }

        public void ClientPosition(IServerModule caller, Client sender)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendPosition(sender);
        }
    }
}
