using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class SetPermissionCommand : Command
    {
        public override string Name { get; protected set; } = "setperm";
        public override string Description { get; protected set; } = "Change Client permission.";
        public override IEnumerable<string> Aliases { get; protected set; } = new [] { "sperm", "sp" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.AdministratorOrHigher;

        public SetPermissionCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                var clientName = arguments[0];
                var permissions = arguments.Skip(1).Where(arg => arg != "," || arg != "|").ToArray();

                var cClient = GetClient(clientName);
                if (cClient == null)
                {
                    client.SendServerMessage($"Player {clientName} not found.");
                    return;
                }

                var flags = new List<PermissionFlags>();
                foreach (var permission in permissions)
                {
                    PermissionFlags flag;
                    if (Enum.TryParse(permission, out flag))
                        flags.Add(flag);
                    else
                        client.SendServerMessage($"Permission {permission} not found.");
                }

                client.Permissions = PermissionFlags.None;
                foreach (var flag in flags)
                    client.Permissions |= flag;

                client.SendServerMessage($"Changed {clientName} permissions!");
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) { client.SendServerMessage($"Correct usage is /{alias} <permission permission permission>"); }
    }
}