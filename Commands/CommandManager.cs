using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Aragas.Core.Wrappers;

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
            var asm = AppDomainWrapper.GetAssembly(typeof (CommandManager));

            var types = asm.DefinedTypes
                .Where(t => typeof(Command).GetTypeInfo().IsAssignableFrom(t))
                .Where(t => !t.IsDefined(typeof(DisableAutoLoadAttribute), true))
                .Where(t => !t.IsAbstract);

            foreach (var command in types.Select(type => (Command) Activator.CreateInstance(type.AsType())))
            {
                command.CommandManager = this;
                Commands.Add(command);
            }
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
