﻿using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class BanCommand : Command
    {
        public override string Name { get; protected set; } = "Ban";
        public override string Description { get; protected set; } = "Ban a Player.";
        public override IEnumerable<string> Aliases { get; protected set; } = new string[] { "b" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.Moderator | PermissionFlags.Administrator | PermissionFlags.Owner;

        public BanCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length >= 1)
            {
                var clientName = arguments[0].ToLower();
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