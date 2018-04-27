using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Services;
using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class KickCommand : Command
    {
        public override string Name => "kick";
        public override string Description => "Kick a Player.";
        public override IEnumerable<string> Aliases => new [] { "k" };
        public override PermissionFlags Permissions => PermissionFlags.ModeratorOrHigher;

        public KickCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                var clientName = arguments[0];
                var cClient = GetClient(clientName);
                if (cClient is null)
                {
                    client.SendServerMessage($"Player {clientName} not found!");
                    return;
                }

                ModuleManager.Kick(cClient, "Kicked by a Moderator or Admin.");
            }
            else if (arguments.Length > 1)
            {
                var clientName = arguments[0];
                var cClient = GetClient(clientName);
                if (cClient is null)
                {
                    client.SendServerMessage($"Player {clientName} not found!");
                    return;
                }

                var reason = string.Join(" ", arguments.Skip(1).ToArray());
                ModuleManager.Kick(cClient, reason);
            }
            else
                client.SendServerMessage("Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <PlayerName> [Reason]");
    }
}