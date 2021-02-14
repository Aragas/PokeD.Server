using System;
using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class UnMuteCommand : Command
    {
        public override string Name => "unmute";
        public override string Description => "Command is disabled";
        public override IEnumerable<string> Aliases => new [] { "um" };
        public override PermissionFlags Permissions => PermissionFlags.UserOrHigher;

        public UnMuteCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            client.SendServerMessage("Command not implemented.");
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
                client.SendServerMessage("Invalid arguments given.");

            /*
            if (!MutedPlayers.ContainsKey(id))
                return MuteStatus.IsNotMuted;

            var muteID = Server.GetClientID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Remove(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.ClientNotFound;
            */
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <PlayerName>");
    }
}