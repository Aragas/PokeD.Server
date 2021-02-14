using Microsoft.Extensions.Hosting;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.P3D;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Database;
using PokeD.Server.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PokeD.Server.Modules
{
    public interface IServerModuleBaseSettings
    {
        bool Enabled { get; }
        ushort Port { get; }
    }

    public abstract class ServerModule : IHostedService, IServerModuleBaseSettings, IUpdatable
    {
        public abstract bool Enabled { get; protected set; }
        public abstract ushort Port { get; protected set; }


        public virtual WorldService World { get; }
        public virtual DatabaseService Database { get; }
        public virtual SecurityService Security { get; }
        public virtual ModuleManagerService ModuleManager { get; }
        public virtual ChatChannelManagerService ChatChannelManager { get; }
        public virtual CommandManagerService CommandManager { get; }

        [ConfigIgnore]
        public virtual bool ClientsVisible => true;

        private readonly ILogger _logger;

        protected ServerModule(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<ServerModule>>();
            World = serviceProvider.GetRequiredService<WorldService>();
            Database = serviceProvider.GetRequiredService<DatabaseService>();
            Security = serviceProvider.GetRequiredService<SecurityService>();
            ModuleManager = serviceProvider.GetRequiredService<ModuleManagerService>();
            ChatChannelManager = serviceProvider.GetRequiredService<ChatChannelManagerService>();
            CommandManager = serviceProvider.GetRequiredService<CommandManagerService>();
        }


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
            if (clientTable == null)
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

           ChatChannelManager.FindByAlias("global").Subscribe(client);

            if (ClientsVisible)
                _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"The player {client.Name} joined the game from IP {client.IP}");
        }
        protected virtual void OnClientLeave(object sender, EventArgs eventArgs)
        {
            var client = sender as Client;
            ((Action<object, ClientLeavedEventArgs>) ModuleManager.ClientLeaved)?.Invoke(this, new ClientLeavedEventArgs(client));

            foreach (var chatChannel in ChatChannelManager.GetChatChannels())
                chatChannel.Unsubscribe(client);

            if (ClientsVisible)
                _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"The player {client.Name} disconnected, playtime was {DateTime.Now - client.ConnectionTime:hh\\:mm\\:ss}");
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
            foreach (var chatChannel in ChatChannelManager.GetChatChannels())
                if (chatChannel.SendMessage(chatMessage))
                    _logger.Log(LogLevel.Information, new EventId(10, "Chat"), chatMessage.Sender.Name, chatChannel.Name, chatMessage.Message);
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

        public bool ExecuteClientCommand(Client client, string command) => CommandManager.ExecuteClientCommand(client, command);

        public abstract Task StartAsync(CancellationToken cancellationToken);
        public abstract Task StopAsync(CancellationToken cancellationToken);

        public abstract void Update();
    }
}