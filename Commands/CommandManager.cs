using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PCLExt.AppDomain;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class DisableAutoLoadAttribute : Attribute { }

    public class CommandManager
    {
        public Server Server { get; }
        public List<Command> Commands { get; } = new List<Command>();

        public CommandManager(Server server, bool autoLoad = true)
        {
            Server = server;

            if(autoLoad)
                AutoLoadCommands();
        }
        private void AutoLoadCommands()
        {
            var types = AppDomain.GetAssembly(typeof(CommandManager)).DefinedTypes
                .Where(typeInfo => typeof(Command).GetTypeInfo().IsAssignableFrom(typeInfo))
                .Where(typeInfo => !typeInfo.IsDefined(typeof(DisableAutoLoadAttribute), true))
                .Where(typeInfo => !typeInfo.IsAbstract);

            foreach (var command in types.Where(type => type != typeof(CommandLua).GetTypeInfo()).Select(type => (Command) Activator.CreateInstance(type.AsType(), this)))
                Commands.Add(command);

            Commands.AddRange(CommandLuaLoader.LoadCommands(this));
        }

        public void HandleCommand(Client client, string alias, string[] arguments)
        {
            var command = FindByName(alias) ?? FindByAlias(alias);
            if (command == null)
            {
                client.SendMessage($@"Invalid command ""{alias}"".");
                return;
            }
            command.Handle(client, alias, arguments);
        }

        public Command FindByName(string name) => Commands.FirstOrDefault(command => command.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        public Command FindByAlias(string alias) => Commands.FirstOrDefault(command => command.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase));
    }
}
