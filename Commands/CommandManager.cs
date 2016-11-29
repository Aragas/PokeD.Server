using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PCLExt.AppDomain;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class CommandManager
    {
        private Server Server { get; }
        public List<Command> Commands { get; } = new List<Command>();

        public CommandManager(Server server, bool autoLoad = true)
        {
            Server = server;

            if (autoLoad)
                AutoLoadCommands();
        }
        private void AutoLoadCommands()
        {
            var types = AppDomain.GetAssembly(typeof(CommandManager)).DefinedTypes
                .Where(typeInfo => typeof(Command).GetTypeInfo().IsAssignableFrom(typeInfo))
                .Where(typeInfo => !typeInfo.IsDefined(typeof(CommandDisableAutoLoadAttribute), true))
                .Where(typeInfo => !typeInfo.IsAbstract);

            foreach (var command in types.Where(type => type != typeof(CommandLua).GetTypeInfo()).Select(type => (Command) Activator.CreateInstance(type.AsType(), Server)))
                Commands.Add(command);

            Commands.AddRange(CommandLuaLoader.LoadCommands(Server));
        }

        public void HandleCommand(Client client, string alias, string[] arguments)
        {
            var command = FindByName(alias) ?? FindByAlias(alias);
            if (command == null)
            {
                client.SendServerMessage($@"Invalid command ""{alias}"".");
                return;
            }

            if (command.Permissions == PermissionFlags.None)
            {
                client.SendServerMessage($@"Command is disabled!");
                return;
            }

            if ((client.Permissions & command.Permissions) == PermissionFlags.None)
            {
                client.SendServerMessage($"You have not the permission to use this command!");
                return;
            }

            command.Handle(client, alias, arguments);
        }

        public Command FindByName(string name) => Commands.FirstOrDefault(command => command.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        public Command FindByAlias(string alias) => Commands.FirstOrDefault(command => command.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));
    }
}