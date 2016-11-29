using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class UnMuteCommand : Command
    {
        public override string Name { get; protected set; } = "unmute";
        public override string Description { get; protected set; } = "";
        public override IEnumerable<string> Aliases { get; protected set; } = new string[] { "um" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.VerifiedOrHigher;

        public UnMuteCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
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

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <PlayerName>"); }
    }
}