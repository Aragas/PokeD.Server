using System;

using PokeD.Core.Services;
using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class ShowPermissionsCommand : Command
    {
        public override string Name => "showperm";
        public override string Description => "Show available Client permissions.";
        public override PermissionFlags Permissions => PermissionFlags.AdministratorOrHigher;

        public ShowPermissionsCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
                client.SendServerMessage(string.Join(",", Enum.GetNames(typeof(PermissionFlags))));
            else if (arguments.Length == 2)
            {
                var clientName = arguments[0];

                var cClient = GetClient(clientName);
                if (cClient == null)
                {
                    client.SendServerMessage($"Player {clientName} not found.");
                    return;
                }

                client.SendServerMessage($"Player {clientName} permissions are {client.Permissions.ToString()}.");
            }
            else
                client.SendServerMessage("Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} [PlayerName]");
    }
}