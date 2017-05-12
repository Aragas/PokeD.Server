using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class MuteCommand : Command
    {
        public override string Name => "mute";
        public override string Description => "";
        public override IEnumerable<string> Aliases => new [] { "mm" };
        public override PermissionFlags Permissions => PermissionFlags.VerifiedOrHigher;

        public MuteCommand(Server server) : base(server) { }

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
                MutedPlayers.Add(id, new List<int>());

            var muteID = Server.GetClientID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Add(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.ClientNotFound;
            */
        }

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <PlayerName>"); }
    }
}