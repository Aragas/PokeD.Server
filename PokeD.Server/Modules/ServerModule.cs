using System;
using System.Collections.Generic;
using System.Linq;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.P3D;
using PokeD.Core.Services;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Components;
using PokeD.Server.Database;
using PokeD.Server.Services;

namespace PokeD.Server.Modules
{
    public interface IServerModuleBaseSettings
    {
        bool Enabled { get; }
        ushort Port { get; }
    }

    public abstract class ServerModule : ServerComponent, IServerModuleBaseSettings
    {
        #region Settings

        public abstract bool Enabled { get; protected set; }
        public abstract ushort Port { get; protected set; }

        #endregion Settings

        protected IServiceContainer Services { get; }

        [ConfigIgnore]
        public virtual WorldService World => Services.GetService<WorldService>();
        [ConfigIgnore]
        public virtual DatabaseService Database => Services.GetService<DatabaseService>();
        [ConfigIgnore]
        public virtual SecurityService Security => Services.GetService<SecurityService>();
        [ConfigIgnore]
        public virtual ModuleManagerService ModuleManager => Services.GetService<ModuleManagerService>();

        [ConfigIgnore]
        public virtual bool ClientsVisible => true;

        private bool IsDisposed { get; set; }


        protected ServerModule(IServiceContainer services, ConfigType configType) : base(configType) { Services = services; }

        
        public abstract void ClientsForeach(Action<IReadOnlyList<Client>> func);
        public abstract TResult ClientsSelect<TResult>(Func<IReadOnlyList<Client>, TResult> func);
        public abstract IReadOnlyList<TResult> ClientsSelect<TResult>(Func<IReadOnlyList<Client>, IReadOnlyList<TResult>> func);

        public TResult AllClientsSelect<TResult>(Func<IReadOnlyList<Client>, TResult> func) => ModuleManager.AllClientsSelect(func);
        public IReadOnlyList<TResult> AllClientsSelect<TResult>(Func<IReadOnlyList<Client>, IReadOnlyList<TResult>> func) => ModuleManager.AllClientsSelect(func);

        
        public Client GetClient(int id) => ModuleManager.GetClient(id);
        public Client GetClient(string name) => ModuleManager.GetClient(name);
        public int GetClientID(string name) => ModuleManager.GetClientID(name);
        public string GetClientName(int id) => ModuleManager.GetClientName(id);

        public virtual bool AssignID(Client client)
        {
            var clientTable = Database.DatabaseGetAll<ClientTable>().FirstOrDefault(table => table.Name == client.Nickname);
            if (clientTable is null)
            {
                clientTable = new ClientTable(client);
                Database.DatabaseSet(clientTable);
                client.Load(clientTable);
                return true;
            }
            else
            {
                client.Load(clientTable);
                return true;
            }
        }

        protected virtual void OnClientReady(object sender, EventArgs eventArgs)
        {
            var client = sender as Client;
           ((Action<object, ClientJoinedEventArgs>) ModuleManager.ClientJoined)?.Invoke(this, new ClientJoinedEventArgs(client));

            Services.GetService<ChatChannelManagerService>().FindByAlias("global").Subscribe(client);

            if (ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} joined the game from IP {client.IP}");
        }
        protected virtual void OnClientLeave(object sender, EventArgs eventArgs)
        {
            var client = sender as Client;
            ((Action<object, ClientLeavedEventArgs>) ModuleManager.ClientLeaved)?.Invoke(this, new ClientLeavedEventArgs(client));

            foreach (var chatChannel in Services.GetService<ChatChannelManagerService>().GetChatChannels())
                chatChannel.UnSubscribe(client);

            if (ClientsVisible)
                Logger.Log(LogType.Event, $"The player {client.Name} disconnected, playtime was {DateTime.Now - client.ConnectionTime:hh\\:mm\\:ss}");
        }

        public void OnServerMessage(string message)
        {
            ClientsForeach(clients =>
            {
                for (var i = clients.Count - 1; i >= 0; i--)
                    clients[i].SendServerMessage(message);
            });
        }

        public void OnClientChatMessage(ChatMessage chatMessage)
        {
            foreach (var chatChannel in Services.GetService<ChatChannelManagerService>().GetChatChannels())
                if (chatChannel.MessageSend(chatMessage))
                    Logger.LogChatMessage(chatMessage.Sender.Name, chatChannel.Name, chatMessage.Message);
        }

        public virtual void OnTradeRequest(Client sender, DataItems monster, Client destClient) => ModuleManager.TradeRequest(sender, monster, destClient, this);
        public virtual void OnTradeConfirm(Client sender, Client destClient) => ModuleManager.TradeConfirm(sender, destClient, this);
        public virtual void OnTradeCancel(Client sender, Client destClient) => ModuleManager.TradeCancel(sender, destClient, this);

        /*
        public virtual void OnBattleRequest(Client sender, Client destClient) { }
        public virtual void OnBattleAccept(Client sender) { }
        public virtual void OnBattleAttack(Client sender) { }
        public virtual void OnBattleItem(Client sender) { }
        public virtual void OnBattleSwitch(Client sender) { }
        public virtual void OnBattleFlee(Client sender) { }
        */

        public virtual void OnPosition(Client sender) { }

        public bool ExecuteClientCommand(Client client, string command) => Services.GetService<CommandManagerService>().ExecuteClientCommand(client, command);
        

        public override bool Start()
        {
            if (!base.Start())
                return false;

            if (!Enabled)
            {
                Logger.Log(LogType.Debug, $"{ComponentName} not enabled!");
                return false;
            }

            return true;
        }
        public override bool Stop()
        {
            if (!base.Stop())
                return false;

            return true;
        }

        public override int GetHashCode() => ComponentName.GetHashCode();

        private bool Equals(ServerModule a, ServerModule b) => string.Equals(a.ComponentName, b.ComponentName, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ServerModule serverModule && Equals(this, serverModule);

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {

                }


                IsDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}