using System;
using System.Collections.Generic;
using System.Diagnostics;

using Org.BouncyCastle.Crypto;

using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core;
using PokeD.Core.Data.PokeD;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public abstract class ServerModule : IUpdatable, IDisposable
    {
        protected abstract string ModuleName { get; }
        protected abstract IConfigFile ModuleConfigFile { get; }

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
        public int GetClientID(string name) => Server.GetClientID(name);
        public string GetClientName(int id) => Server.GetClientName(id);

        public bool ExecuteClientCommand(Client client, string command) => Server.ExecuteClientCommand(client, command);

        private Dictionary<int, Stopwatch> UpdateWatches { get; } = new Dictionary<int, Stopwatch>();
        public void ClientUpdate(Client client, bool forceUpdate = false)
        {
            if (client.ID == 0 || !UpdateWatches.ContainsKey(client.ID))
                return;


            if (forceUpdate || UpdateWatches[client.ID].ElapsedMilliseconds >= 2000)
            {
                Server.DatabaseUpdate(new ClientTable(client));

                UpdateWatches[client.ID].Reset();
                UpdateWatches[client.ID].Start();
            }
        }
        public void ClientLoad(Client client) => client.LoadFromDB(Server.DatabaseGet<ClientTable>(client.ID));
        //public ClientTable ClientLoad(Client client) => Server.DatabaseGet<ClientTable>(client.ID);


        public virtual bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(ModuleConfigFile, this);
            if (!status)
                Logger.Log(LogType.Warning, $"Failed to load {ModuleName} settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"{ModuleName} not enabled!");
                return false;
            }

            return true;
        }
        public virtual bool Stop()
        {
            var status = FileSystemExtensions.SaveConfig(ModuleConfigFile, this);
            if (!status)
                Logger.Log(LogType.Warning, $"Failed to save {ModuleName} settings!");

            return true;
        }

        public virtual void AddClient(Client client)
        {
            if(!UpdateWatches.ContainsKey(client.ID))
                UpdateWatches.Add(client.ID, Stopwatch.StartNew());

            Server.NotifyClientConnected(this, client);
        }
        public virtual void RemoveClient(Client client, string reason = "")
        {
            if (UpdateWatches.ContainsKey(client.ID))
                UpdateWatches.Remove(client.ID);

            Server.NotifyClientDisconnected(this, client);
        }

        public abstract void Update();

        public abstract void ClientConnected(Client client);
        public abstract void ClientDisconnected(Client client);

        public void SendServerMessage(string message)
        {
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i].SendServerMessage(message);
        }
        public void SendChatMessage(ChatMessage chatMessage) => Server.NotifyServerGlobalMessage(this, chatMessage);
        public void SendPrivateMessage(Client destClient, ChatMessage chatMessage) => destClient.SendPrivateMessage(chatMessage);

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