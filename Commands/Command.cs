using System.Collections.Generic;

using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Commands
{
    public abstract class Command
    {
        protected CommandManager CommandManager { get; }
        protected World World => CommandManager.Server.World;

        public abstract string Name { get; protected set; }
        public abstract string Description { get; protected set; }
        public virtual IEnumerable<string> Aliases { get; protected set; } = new string[0];

        public Command(CommandManager commandManager) { CommandManager = commandManager; }

        public virtual void Handle(Client client, string alias, string[] arguments) { Help(client, alias); }

        public virtual void Help(Client client, string alias) { client.SendMessage($@"Command ""{alias}"" is not functional!"); }
    }
}
