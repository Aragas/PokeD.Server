using System;
using System.Collections.Generic;

using PokeD.Server.Clients;
using PokeD.Server.Data;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class SetWeatherCommand : Command
    {
        public override string Name { get; protected set; } = "setweather";
        public override string Description { get; protected set; } = "Set World Weather.";
        public override IEnumerable<string> Aliases { get; protected set; } = new string[] { "sw" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.ModeratorOrHigher;

        public SetWeatherCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                Weather weather;
                if (Enum.TryParse(arguments[0], true, out weather))
                {
                    World.Weather = weather;
                    client.SendServerMessage($"Set Weather to {weather}!");
                }
                else
                    client.SendServerMessage($"Weather '{weather}' not found!");
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) { client.SendServerMessage($"Correct usage is /{alias} <Weather>"); }
    }
}