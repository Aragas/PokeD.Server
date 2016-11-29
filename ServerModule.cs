using System;
using System.Collections.Generic;

using Aragas.Network.Packets;

using Org.BouncyCastle.Crypto;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server
{
    public abstract class ServerModule : IUpdatable, IDisposable
    {
        protected Server Server { get; }
        [ConfigIgnore]
        public World World => Server.World;
        [ConfigIgnore]
        public AsymmetricCipherKeyPair RSAKeyPair => Server.RSAKeyPair;
        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public virtual bool ClientsVisible { get; } = true;


        public abstract bool Enabled { get; protected set; }
        public abstract ushort Port { get; protected set; }


        public ServerModule(Server server) { Server = server; }


        public IEnumerable<Client> GetAllClients() => Server.GetAllClients();
        public Client GetClient(int id) => Server.GetClient(id);
        public Client GetClient(string name) => Server.GetClient(name);
        public int GetClientID(string name) => Server.GetClientID(name);
        public string GetClientName(int id) => Server.GetClientName(id);

        public bool ExecuteClientCommand(Client client, string command) => Server.ExecuteClientCommand(client, command);

        public void SaveClient(Client client) => Server.DatabasePlayerSave(client);


        public abstract bool Start();
        public abstract void Stop();

        public abstract void StartListen();
        public abstract void CheckListener();

        public virtual void AddClient(Client client) { Server.NotifyClientConnected(this, client); }
        public virtual void RemoveClient(Client client, string reason = "") { Server.NotifyClientDisconnected(this, client); }

        public abstract void Update();

        public abstract void ClientConnected(Client client);
        public abstract void ClientDisconnected(Client client);

        public abstract void SendPacketToAll(Packet packet);

        public void SendServerMessage(string message)
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i].SendServerMessage(message);
        }
        public void SendChatMessage(ChatMessage chatMessage) { Server.NotifyServerGlobalMessage(this, chatMessage); }
        public void SendPrivateMessage(Client destClient, ChatMessage chatMessage) { destClient.SendPrivateMessage(chatMessage); }

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