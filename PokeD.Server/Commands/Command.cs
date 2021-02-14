using Microsoft.Extensions.DependencyInjection;

using PokeD.Core.Data;
using PokeD.Core.Data.P3D;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;
using PokeD.Server.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.Commands
{
    public abstract class Command
    {
        private sealed class OfflineServerModule : ServerModule
        {
            public OfflineServerModule() : base(null) { }

            public override bool Enabled { get; protected set; }
            public override ushort Port { get; protected set; }

            public override void ClientsForeach(Action<IReadOnlyList<Client>> func) { }
            public override TResult ClientsSelect<TResult>(Func<IReadOnlyList<Client>, TResult> func) => default;
            public override IReadOnlyList<TResult> ClientsSelect<TResult>(Func<IReadOnlyList<Client>, IReadOnlyList<TResult>> func) => new List<TResult>();

            public override void OnTradeRequest(Client sender, DataItems monster, Client destClient) { }
            public override void OnTradeConfirm(Client sender, Client destClient) { }
            public override void OnTradeCancel(Client sender, Client destClient) { }

            public override void OnPosition(Client sender) { }
            public override Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public override Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public override void Update() {}
        }
        private sealed class OfflineClient : Client
        {
            private int _id;
            public override int ID
            {
                get => _id;
                set { _id = value; _database.DatabaseUpdate(new ClientTable(this)); }
            }

            private string _nickname;
            public override string Nickname
            {
                get => _nickname;
                protected set { _nickname = value; _database.DatabaseUpdate(new ClientTable(this)); }
            }

            private Prefix _prefix;
            public override Prefix Prefix
            {
                get => _prefix;
                protected set { _prefix = value; _database.DatabaseUpdate(new ClientTable(this)); }
            }

            private string _passwordHash;
            public override string PasswordHash
            {
                get => _passwordHash;
                set { _passwordHash = value; _database.DatabaseUpdate(new ClientTable(this)); }
            }

            private PermissionFlags _permissions;
            public override PermissionFlags Permissions
            {
                get => _permissions;
                set { _permissions = value; _database.DatabaseUpdate(new ClientTable(this)); }
            }

            public override Vector3 Position { get; set; } = Vector3.Zero;
            public override string LevelFile { get; set; } = string.Empty;
            public override string IP { get; } = string.Empty;
            public override DateTime ConnectionTime { get; } = DateTime.MinValue;
            public override CultureInfo Language { get; }
            public override GameDataPacket GetDataPacket() => null;

            private readonly DatabaseService _database;

            public OfflineClient(DatabaseService database, ClientTable clientTable) : base(new OfflineServerModule())
            {
                _database = database;

                ID = clientTable.ClientID.Value;
                Nickname = clientTable.Name;
                Prefix = clientTable.Prefix;
                PasswordHash = clientTable.PasswordHash;
                Permissions = clientTable.Permissions;
            }


            public override bool RegisterOrLogIn(string passwordHash) => false;
            public override bool ChangePassword(string oldPassword, string newPassword) => false;

            public override void SendPacket<TPacket>(TPacket packet) { }

            public override void SendChatMessage(ChatChannel chatChannel, ChatMessage chatMessage) { }
            public override void SendServerMessage(string text) { }
            public override void SendPrivateMessage(ChatMessage chatMessage) { }

            public override void Load(ClientTable data) { }


            public override void Update() { }
        }

        // -- Should be hidden ideally
        protected ModuleManagerService ModuleManager { get; }
        protected CommandManagerService CommandManager { get; }
        protected ChatChannelManagerService ChatChannelManager { get; }
        // -- Should be hidden ideally

        protected WorldService World { get; }
        protected DatabaseService Database { get; }

        protected Client GetClient(string name)
        {
            var client = ModuleManager.GetClient(name);
            if (client != null)
                return client;

            var clientTable = Database.DatabaseFind<ClientTable>(c => c.Name == name);
            return clientTable == null ? null : new OfflineClient(Database, clientTable);
        }


        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual IEnumerable<string> Aliases { get; } = Array.Empty<string>();
        public virtual PermissionFlags Permissions { get; } = PermissionFlags.None;
        public virtual bool LogCommand { get; } = true;

        protected Command(IServiceProvider serviceProvider)
        {
            ModuleManager = serviceProvider.GetRequiredService<ModuleManagerService>();
            CommandManager = serviceProvider.GetRequiredService<CommandManagerService>();
            ChatChannelManager = serviceProvider.GetRequiredService<ChatChannelManagerService>();
            World = serviceProvider.GetRequiredService<WorldService>();
            Database = serviceProvider.GetRequiredService<DatabaseService>();
        }

        public virtual void Handle(Client client, string alias, string[] arguments) { Help(client, alias); }

        public virtual void Help(Client client, string alias) { client.SendServerMessage($@"Command ""{alias}"" is not functional!"); }
    }
}