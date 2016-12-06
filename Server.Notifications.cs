using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server
    {
        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the <see cref="Client"/> connection.
        /// </summary>
        public void NotifyClientConnected(ServerModule caller, Client client)
        {
            ChatClientConnected(client);

            foreach (var module in Modules.Where(module => caller != module))
                module.ClientConnected(client);

            if(caller.ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} joined the game from IP {client.IP}");
        }
        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the <see cref="Client"/> disconnection.
        /// </summary>
        public void NotifyClientDisconnected(ServerModule caller, Client client)
        {
            ChatClientDisconnected(client);

            foreach (var module in Modules.Where(module => caller != module))
                module.ClientDisconnected(client);

            if (caller.ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} disconnected, playtime was {DateTime.Now - client.ConnectionTime:hh\\:mm\\:ss}");
        }

        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the server message.
        /// </summary>
        public void NotifyServerMessage(ServerModule caller, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendServerMessage(message);

            Logger.Log(LogType.Chat, message);
        }
        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the <see cref="Client"/> global message.
        /// </summary>
        public void NotifyServerGlobalMessage(ServerModule caller, ChatMessage chatMessage)
        {
            ChatClientSentMessage(chatMessage);
        }

        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the <see cref="Client"/> trade offer.
        /// </summary>
        public void NotifyClientTradeOffer(ServerModule caller, Client client, Monster monster, Client destClient)
        {
            OnClientTradeOffer(client, monster, destClient);

            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeRequest(client, monster, destClient, true);
        }
        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the <see cref="Client"/> trade confirmation.
        /// </summary>
        public void NotifyClientTradeConfirm(ServerModule caller, Client client, Client destClient)
        {
            OnClientTradeConfirm(client, destClient);

            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeConfirm(client, destClient, true);
        }
        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the <see cref="Client"/> trade cancellation.
        /// </summary>
        public void NotifyClientTradeCancel(ServerModule caller, Client client, Client destClient)
        {
            OnClientTradeCancel(client, destClient);

            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeCancel(client, destClient, true);
        }

        /// <summary>
        /// Notify every <see cref="ServerModule"/> about the <see cref="Client.Position"/>.
        /// </summary>
        public void NotifyClientPosition(ServerModule caller, Client sender)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendPosition(sender, true);
        }


        private List<TradeInstance> CurrentTrades { get; } = new List<TradeInstance>();

        private void OnClientTradeOffer(Client client, Monster monster, Client destClient)
        {
            if (!CurrentTrades.Any(t => t.Equals(client.Id, destClient.Id)))
                CurrentTrades.Add(new TradeInstance { Client0Id = client.Id, Client1Id = destClient.Id });

            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(client.Id, destClient.Id));
            if (trade != null)
            {
                if(trade.Client0Id == client.Id)
                    trade.Client0Monster = monster;

                if (trade.Client1Id == client.Id)
                    trade.Client1Monster = monster;
            }
        }
        private void OnClientTradeConfirm(Client client, Client destClient)
        {
            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(client.Id, destClient.Id));
            if (trade != null)
            {
                if (trade.Client0Id == client.Id)
                    trade.Client0Confirmed = true;
                if (trade.Client1Id == client.Id)
                    trade.Client1Confirmed = true;

                if (trade.Client0Confirmed && trade.Client1Confirmed)
                {
                    DatabaseSet(new TradeTable(Database, trade));
                    CurrentTrades.Remove(trade);
                }
            }
        }
        private void OnClientTradeCancel(Client client, Client destClient)
        {
            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(client.Id, destClient.Id));
            if (trade != null)
                CurrentTrades.Remove(trade);
        }
    }
}