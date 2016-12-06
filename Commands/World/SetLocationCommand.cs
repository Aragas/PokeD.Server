using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class SetLocationCommand : Command
    {
        public override string Name { get; protected set; } = "setlocation";
        public override string Description { get; protected set; } = "Set World Location.";
        public override IEnumerable<string> Aliases { get; protected set; } = new [] { "sl" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.ModeratorOrHigher;

        public SetLocationCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                World.Location = arguments[0];
                World.UseLocation = true;
                client.SendServerMessage($"Set Location to {World.Location}!");
                client.SendServerMessage("Enabled Location!");
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) { client.SendServerMessage($"Correct usage is /{alias} <Location>"); }
    }
}