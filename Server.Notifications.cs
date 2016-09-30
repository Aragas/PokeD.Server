﻿using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server
{
    public partial class Server
    {
        /// <summary>
        /// Default <see cref="Client"/> for internal <see cref="Server"/> handling.
        /// </summary>
        private static Client ServerClient { get; } = new ServerClient();


        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client"/> connection.
        /// </summary>
        public void NotifyClientConnected(IServerModule caller, Client client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.ClientConnected(client);

            if(caller.ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} joined the game from IP {client.IP}");
        }
        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client"/> disconnection.
        /// </summary>
        public void NotifyClientDisconnected(IServerModule caller, Client client)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.ClientDisconnected(client);

            if (caller.ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} disconnected, playtime was {DateTime.Now - client.ConnectionTime:hh\\:mm\\:ss}");
        }

        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the server message.
        /// </summary>
        public void NotifyServerMessage(IServerModule caller, Client sender, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendServerMessage(sender, message, true);

            Logger.Log(LogType.Chat, message);
        }
        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client"/> private message.
        /// </summary>
        public void NotifyClientPrivateMessage(IServerModule caller, Client sender, Client destClient, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendPrivateMessage(sender, destClient, message, true);
        }
        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client"/> global message.
        /// </summary>
        public void NotifyServerGlobalMessage(IServerModule caller, Client sender, string message)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendGlobalMessage(sender, message, true);

            Logger.Log(LogType.Chat, $"<{sender.Name}> {message}");
        }

        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client"/> trade offer.
        /// </summary>
        public void NotifyClientTradeOffer(IServerModule caller, Client client, Monster monster, Client destClient)
        {
            OnClientTradeOffer(client, monster, destClient);

            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeRequest(client, monster, destClient, true);
        }
        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client"/> trade confirmation.
        /// </summary>
        public void NotifyClientTradeConfirm(IServerModule caller, Client client, Client destClient)
        {
            OnClientTradeConfirm(client, destClient);

            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeConfirm(client, destClient, true);
        }
        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client"/> trade cancellation.
        /// </summary>
        public void NotifyClientTradeCancel(IServerModule caller, Client client, Client destClient)
        {
            OnClientTradeCancel(client, destClient);

            foreach (var module in Modules.Where(module => caller != module))
                module.SendTradeCancel(client, destClient, true);
        }

        /// <summary>
        /// Notify every <see cref="IServerModule"/> about the <see cref="Client.Position"/>.
        /// </summary>
        public void NotifyClientPosition(IServerModule caller, Client sender)
        {
            foreach (var module in Modules.Where(module => caller != module))
                module.SendPosition(sender, true);
        }


        private List<TradeInstance> CurrentTrades { get; } = new List<TradeInstance>();

        private void OnClientTradeOffer(Client client, Monster monster, Client destClient)
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
        private void OnClientTradeConfirm(Client client, Client destClient)
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
        private void OnClientTradeCancel(Client client, Client destClient)
        {
            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(client.ID, destClient.ID));
            if (trade != null)
                CurrentTrades.Remove(trade);
        }
    }
}