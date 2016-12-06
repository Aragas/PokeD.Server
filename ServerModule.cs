using System;
using System.Collections.Generic;
using System.Diagnostics;

using Aragas.Network.Packets;

using Org.BouncyCastle.Crypto;

using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public abstract class ServerModule : IUpdatable, IDisposable
    {
        protected abstract string ModuleFileName { get; }
        protected Server Server { get; }

        #region Settings

        public abstract bool Enabled { get; protected set; }
        public abstract ushort Port { get; protected set; }

        #endregion Settings

        [ConfigIgnore]
        public World World => Server.World;
        [ConfigIgnore]
        public AsymmetricCipherKeyPair RsaKeyPair => Server.RSAKeyPair;
        [ConfigIgnore]
        public List<Client> Clients { get; } = new List<Client>();
        [ConfigIgnore]
        public virtual bool ClientsVisible { get; } = true;


        public ServerModule(Server server) { Server = server; }


        public IEnumerable<Client> GetAllClients() => Server.GetAllClients();
        public Client GetClient(int id) => Server.GetClient(id);
        public Client GetClient(string name) => Server.GetClient(name);
        public int GetClientId(string name) => Server.GetClientId(name);
        public string GetClientName(int id) => Server.GetClientName(id);

        public bool ExecuteClientCommand(Client client, string command) => Server.ExecuteClientCommand(client, command);

        private Stopwatch ClientSaveWatch { get; } = Stopwatch.StartNew();
        public void ClientUpdate(Client client, bool forceUpdate = false)
        {
            if (client.Id == 0)
                return;

            if (ClientSaveWatch.ElapsedMilliseconds < 2000 && !forceUpdate)
                return;

            Server.DatabaseUpdate(new ClientTable(client));

            ClientSaveWatch.Reset();
            ClientSaveWatch.Start();
        }
        public void ClientLoad(Client client) => client.LoadFromDB(Server.DatabaseGet<ClientTable>(client.Id));
        //public ClientTable ClientLoad(Client client) => Server.DatabaseGet<ClientTable>(client.Id);


        public virtual bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(Server.ConfigType, ModuleFileName, this);
            if (!status)
                Logger.Log(LogType.Warning, $"Failed to load {ModuleFileName} settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"{ModuleFileName} not enabled!");
                return false;
            }

            return true;
        }
        public virtual bool Stop()
        {
            var status = FileSystemExtensions.SaveConfig(Server.ConfigType, ModuleFileName, this);
            if (!status)
                Logger.Log(LogType.Warning, $"Failed to save {ModuleFileName} settings!");

            return true;
        }

        public virtual void AddClient(Client client) { Server.NotifyClientConnected(this, client); }
        public virtual void RemoveClient(Client client, string reason = "") { Server.NotifyClientDisconnected(this, client); }

        public abstract void Update();

        public abstract void ClientConnected(Client client);
        public abstract void ClientDisconnected(Client client);

        public abstract void SendPacketToAll(Packet packet);

        public void SendServerMessage(string message)
        {
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i].SendServerMessage(message);
        }
        public void SendChatMessage(ChatMessage chatMessage) { Server.NotifyServerGlobalMessage(this, chatMessage); }
        public void SendPrivateMessage(Client destClient, ChatMessage chatMessage) { destClient.SendPrivateMessage(chatMessage); }

        public abstract void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false);
        public abstract void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false);
        public abstract void SendTradeCancel(Client sender, Client destClient, bool fromServer = false);

        //public abstract void SendBattleRequest(Client sender, Client destClient, bool fromServer = false);
        //public abstract void SendBattleAccept(Client sender, bool fromServer = false);
        //public abstract void SendBattleAttack(Client sender, bool fromServer = false);
        //public abstract void SendBattleItem(Client sender, bool fromServer = false);
        //public abstract void SendBattleSwitch(Client sender, bool fromServer = false);
        //public abstract void SendBattleFlee(Client sender, bool fromServer = false);

        public abstract void SendPosition(Client sender, bool fromServer = false);

        public abstract void Dispose();
    }
}