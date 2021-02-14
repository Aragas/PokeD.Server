using System;
using System.Collections.Generic;

using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Commands
{
    public class SetSeasonCommand : Command
    {
        public override string Name => "setseason";
        public override string Description => "Set World Season.";
        public override IEnumerable<string> Aliases => new [] { "ss" };
        public override PermissionFlags Permissions => PermissionFlags.ModeratorOrHigher;

        public SetSeasonCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                if (Enum.TryParse(arguments[0], true, out Season season))
                {
                    World.Season = season;
                    client.SendServerMessage($"Set Season to {season}!");
                }
                else
                    client.SendServerMessage($"Season '{season}' not found!");
            }
            else
                client.SendServerMessage("Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <Season>");
    }
}