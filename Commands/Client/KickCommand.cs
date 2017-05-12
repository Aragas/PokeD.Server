using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class KickCommand : Command
    {
        public override string Name => "kick";
        public override string Description => "Kick a Player.";
        public override IEnumerable<string> Aliases => new [] { "k" };
        public override PermissionFlags Permissions => PermissionFlags.ModeratorOrHigher;

        public KickCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length >= 1)
            {
                var clientName = arguments[0];
                var cClient = GetClient(clientName);
                if (cClient == null)
                {
                    client.SendServerMessage($"Player {clientName} not found!");
                    return;
                }

                var reason = arguments.Length > 1 ? arguments[1] : "";
                cClient.Kick(reason);
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <PlayerName> [Reason]"); }
    }
}