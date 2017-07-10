using System.Collections.Generic;
using System.Linq;
using PokeD.Core.Services;
using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class BanCommand : Command
    {
        public override string Name => "ban";
        public override string Description => "Ban a Player.";
        public override IEnumerable<string> Aliases => new [] { "b" };
        public override PermissionFlags Permissions => PermissionFlags.ModeratorOrHigher;

        public BanCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 3)
            {
                var clientName = arguments[0];
                var cClient = GetClient(clientName);
                if (cClient == null)
                {
                    client.SendServerMessage($"Player {clientName} not found!");
                    return;
                }

                if (!int.TryParse(arguments[1], out int minutes))
                {
                    client.SendServerMessage($"Invalid minutes given.");
                    return;
                }

                var reason = arguments[2].TrimStart('"').TrimEnd('"');
                ModuleManager.Ban(cClient, minutes, reason);
            }
            else if (arguments.Length > 3)
            {
                var clientName = arguments[0];
                var cClient = GetClient(clientName);
                if (cClient == null)
                {
                    client.SendServerMessage($"Player {clientName} not found!");
                    return;
                }

                if (!int.TryParse(arguments[1], out int minutes))
                {
                    client.SendServerMessage($"Invalid minutes given.");
                    return;
                }

                var reason = string.Join(" ", arguments.Skip(2).ToArray());
                ModuleManager.Ban(cClient, minutes, reason);
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <PlayerName> [Reason]");
    }
}