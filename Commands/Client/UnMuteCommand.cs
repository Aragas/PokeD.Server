﻿using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class UnMuteCommand : Command
    {
        public override string Name { get; protected set; } = "unmute";
        public override string Description { get; protected set; } = "";
        public override IEnumerable<string> Aliases { get; protected set; } = new [] { "um" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.VerifiedOrHigher;

        public UnMuteCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            client.SendServerMessage($"Command not implemented.");
            return;

            if (arguments.Length == 1)
            {
                var clientName = arguments[0];
                var cClient = GetClient(clientName);
                if (cClient == null)
                {
                    client.SendServerMessage($"Player {clientName} not found!");
                    return;
                }

            }
            else
                client.SendServerMessage($"Invalid arguments given.");

            /*
            if (!MutedPlayers.ContainsKey(id))
                return MuteStatus.IsNotMuted;

            var muteId = Server.GetClientId(muteName);
            if (id == muteId)
                return MuteStatus.MutedYourself;

            if (muteId != -1)
            {
                MutedPlayers[id].Remove(muteId);
                return MuteStatus.Completed;
            }

            return MuteStatus.ClientNotFound;
            */
        }

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <PlayerName>"); }
    }
}