using System;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Chat;
using PokeD.Server.Clients;

namespace PokeD.Server
{
    public abstract class ServerModule : IUpdatable, IDisposable
    {
        [ConfigIgnore]
        public Server Server { get; }
        public abstract bool Enabled { get; protected set; }
        public abstract ushort Port { get; protected set; }
        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public virtual bool ClientsVisible { get; } = true;


        public ServerModule(Server server) { Server = server; }

        public abstract bool Start();
        public abstract void Stop();

        public abstract void StartListen();
        public abstract void CheckListener();

        public abstract void RemoveClient(Client client, string reason = "");

        public abstract void Update();

        public abstract void ClientConnected(Client client);
        public abstract void ClientDisconnected(Client client);

        public void SendServerMessage(string message)
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i].SendServerMessage(message);
        }
        public void SendChatMessage(ChatMessage chatMessage) { Server.NotifyServerGlobalMessage(this, chatMessage); }
        public abstract void SendPrivateMessage(Client sender, Client destClient, string message, bool fromServer = false);
        //public abstract void SendServerMessage(Client sender, string message);
        //public abstract void SendGlobalMessage(Client sender, string message, bool fromServer = false);
        //public abstract void SendServerMessage(Client sender, string message, bool fromServer = false);
        //public void SendServerMessage(string message) { Server.ChatClientOnMessage(new ChatMessage(Server.ServerClient, message)); }

        public abstract void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false);
        public abstract void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false);
        public abstract void SendTradeCancel(Client sender, Client destClient, bool fromServer = false);

        //public abstract void SendBattleRequest(Client sender, Client destClient);
        //public abstract void SendBattleAccept(Client sender);
        //public abstract void SendBattleAttack(Client sender);
        //public abstract void SendBattleItem(Client sender);
        //public abstract void SendBattleSwitch(Client sender);
        //public abstract void SendBattleFlee(Client sender);

        public abstract void SendPosition(Client sender, bool fromServer = false);

        public abstract void Dispose();
    }
}