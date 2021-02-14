using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PokeD.Core.Data;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.Services
{
    public sealed class CommandManagerService : IHostedService, IDisposable
    {
        private sealed class ServerClient : Client
        {
            public override int ID { get; set; } = 0;
            public override string Nickname { get; protected set; } = "SERVER";
            public override Prefix Prefix { get; protected set; } = Prefix.NONE;
            public override string PasswordHash { get; set; } = string.Empty;
            public override Vector3 Position { get; set; } = Vector3.Zero;
            public override string LevelFile { get; set; } = string.Empty;
            public override PermissionFlags Permissions { get; set; } = PermissionFlags.Server;
            public override string IP { get; } = string.Empty;
            public override DateTime ConnectionTime { get; } = DateTime.MinValue;
            public override CultureInfo Language { get; } = CultureInfo.InvariantCulture;
            public override GameDataPacket? GetDataPacket() => null;

            public ServerClient() : base(null!) { }

            public override void SendPacket<TPacket>(TPacket func) { }
            public override void SendChatMessage(ChatChannel chatChannel, ChatMessage chatMessage)
            {
                // TODO:
                //_logger.LogChatMessage(chatMessage.Sender.Name, chatChannel.Name, chatMessage.Message);
            }
            public override void SendServerMessage(string text)
            {
                // TODO:
                //_logger.Log(LogType.Command, text);
            }
            public override void SendPrivateMessage(ChatMessage chatMessage) { }

            public override void Load(ClientTable data) { }


            public override Task UpdateAsync(CancellationToken ct) => Task.CompletedTask;
        }

        private List<Command> Commands { get; } = new();

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public CommandManagerService(ILogger<CommandManagerService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Return <see langword="false"/> if <see cref="Command"/> not found.
        /// </summary>
        public bool ExecuteClientCommand(Client client, string message)
        {
            var commandWithoutSlash = message.TrimStart('/');

            var messageArray = new Regex(@"[ ](?=(?:[^""]*""[^""]*"")*[^""]*$)").Split(commandWithoutSlash).Select(str => str.TrimStart('"').TrimEnd('"')).ToArray();
            //var messageArray = commandWithoutSlash.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (messageArray.Length == 0)
                return false; // command not found

            var alias = messageArray[0];
            var trimmedMessageArray = messageArray.Skip(1).ToArray();

            if (!Commands.Any(c => c.Name == alias || c.Aliases.Any(a => a == alias)))
                return false; // command not found

            HandleCommand(client, alias, trimmedMessageArray);

            return true;
        }

        /// <summary>
        /// Return <see langword="false"/> if <see cref="Command"/> not found.
        /// </summary>
        public bool ExecuteServerCommand(string message) => ExecuteClientCommand(new ServerClient(), message);

        private void HandleCommand(Client client, string alias, string[] arguments)
        {
            var command = FindByName(alias) ?? FindByAlias(alias);
            if (command == null)
            {
                client.SendServerMessage($@"Invalid command ""{alias}"".");
                return;
            }

            if(command.LogCommand && (client.Permissions & PermissionFlags.UnVerified) == 0)
                _logger.Log(LogLevel.Information, new EventId(40, "Command"), client.Name, $"/{alias} {string.Join(" ", arguments)}");

            if (command.Permissions == PermissionFlags.None)
            {
                client.SendServerMessage("Command is disabled!");
                return;
            }

            if ((client.Permissions & command.Permissions) == PermissionFlags.None)
            {
                client.SendServerMessage("You have not the permission to use this command!");
                return;
            }

            command.Handle(client, alias, arguments);
        }

        public Command? FindByName(string name) => Commands.Find(command => command.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        public Command? FindByAlias(string alias) => Commands.Find(command => command.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));

        public IReadOnlyList<Command> GetCommands() => Commands;


        private void LoadCommands()
        {
            var types = typeof(CommandManagerService).GetTypeInfo().Assembly.DefinedTypes
                .Where(typeInfo => typeof(Command).GetTypeInfo().IsAssignableFrom(typeInfo) &&
                !typeInfo.IsDefined(typeof(CommandDisableAutoLoadAttribute), true) &&
                !typeInfo.IsAbstract);

            foreach (var command in types.Where(type => !Equals(type, typeof(ScriptCommand).GetTypeInfo())).Select(type => (Command) Activator.CreateInstance(type.AsType(), _serviceProvider)))
                Commands.Add(command);


            var scriptCommandLoaderTypes = typeof(CommandManagerService).GetTypeInfo().Assembly.DefinedTypes
                .Where(typeInfo => typeof(ScriptCommandLoader).GetTypeInfo().IsAssignableFrom(typeInfo) &&
                !typeInfo.IsDefined(typeof(CommandDisableAutoLoadAttribute), true) &&
                !typeInfo.IsAbstract);

            foreach (var scriptCommandLoader in scriptCommandLoaderTypes.Where(type => type != typeof(ScriptCommandLoader).GetTypeInfo()).Select(type => (ScriptCommandLoader) Activator.CreateInstance(type.AsType())))
                Commands.AddRange(scriptCommandLoader.LoadCommands(_serviceProvider));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Loading Commands...");
            LoadCommands();
            _logger.LogDebug("Loaded Commands.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Unloading Commands...");
            Commands.Clear();
            _logger.LogDebug("Unloaded Commands.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Commands.Clear();
        }
    }
}