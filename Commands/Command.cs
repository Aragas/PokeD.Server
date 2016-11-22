using System.Collections.Generic;
using System.Linq;
using PokeD.Server.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Commands
{
    public abstract class Command
    {
        private Server Server { get; } 
        protected World World => Server.World;
        protected CommandManager CommandManager => Server.CommandManager;
        protected ChatChannelManager ChatChannelManager => Server.ChatChannelManager;

        public IEnumerable<Client> GetAllClients() => Server.GetAllClients();
        public Client GetClient(int id) => Server.GetClient(id);
        public Client GetClient(string name) => Server.GetClient(name);


        public abstract string Name { get; protected set; }
        public abstract string Description { get; protected set; }
        public virtual IEnumerable<string> Aliases { get; protected set; } = new string[0];
        public virtual PermissonFlags Permissons { get; protected set; } = PermissonFlags.Default;

        public Command(Server server) { Server = server; }

        public virtual void Handle(Client client, string alias, string[] arguments) { Help(client, alias); }

        public virtual void Help(Client client, string alias) { client.SendServerMessage($@"Command ""{alias}"" is not functional!"); }
    }
}