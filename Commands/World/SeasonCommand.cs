using System;
using System.Collections.Generic;

using PokeD.Server.Clients;
using PokeD.Server.Data;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class SeasonCommand : Command
    {
        public override string Name { get; protected set; } = "setseason";
        public override string Description { get; protected set; } = "Set World Season.";
        public override IEnumerable<string> Aliases { get; protected set; } = new string[] { "ss" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.Moderator | PermissionFlags.Administrator | PermissionFlags.Owner;

        public SeasonCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length >= 1)
            {
                Season season;
                if (Enum.TryParse(arguments[0], true, out season))
                {
                    World.Season = season;
                    client.SendServerMessage($"Set Season to {season}!");
                }
                else
                    client.SendServerMessage($"Season '{season}' not found!");
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) { client.SendServerMessage($"Correct usage is /{alias} <Season>"); }
    }
}