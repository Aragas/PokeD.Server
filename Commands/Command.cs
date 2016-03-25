using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public abstract class Command
    {
        public CommandManager CommandManager { protected get; set; }

        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual IEnumerable<string> Aliases => new string[0];

        public virtual void Handle(Client client, string alias, string[] arguments) { Help(client, alias); }

        public virtual void Help(Client client, string alias) { client.SendMessage($@"Command ""{alias}"" is not functional!"); }
    }
}
