using System;
using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class ChangePermissionCommand : Command
    {
        public override string Name { get; protected set; } = "changeperm";
        public override string Description { get; protected set; } = "Change Client permission.";
        public override IEnumerable<string> Aliases { get; protected set; } = new string[] { "cperm", "cp" };

        public ChangePermissionCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if(client.Permissions != PermissonFlags.Administrator || client.Permissions != PermissonFlags.Owner)
            {
                client.SendServerMessage($"You have not the permission to use this command!");
                return;
            }

            if (arguments.Length == 2)
            {
                var clientName = arguments[0];
                var permission = arguments[1];

                var cClient = GetClient(clientName);
                if (cClient == null)
                {
                    client.SendServerMessage($"Player {clientName} not found.");
                    return;
                }
                PermissonFlags flags;
                if (Enum.TryParse(permission, out flags))
                {
                    cClient.Permissions = flags;
                }
                else
                    client.SendServerMessage($"Permission {permission} not found.");
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) { client.SendServerMessage($"Correct usage is"); }
    }
}