using System.Collections.Generic;

using PokeD.Core.Services;
using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class MuteCommand : Command
    {
        public override string Name => "mute";
        public override string Description => "Command is disabled";
        public override IEnumerable<string> Aliases => new [] { "mm" };
        public override PermissionFlags Permissions => PermissionFlags.UserOrHigher;

        public MuteCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            client.SendServerMessage("Command not implemented.");
            return;

            if (arguments.Length == 1)
            {
                var clientName = arguments[0];
                var cClient = GetClient(clientName);
                if (cClient is null)
                {
                    client.SendServerMessage($"Player {clientName} not found!");
                    return;
                }

            }
            else
                client.SendServerMessage("Invalid arguments given.");



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

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <PlayerName>");
    }
}