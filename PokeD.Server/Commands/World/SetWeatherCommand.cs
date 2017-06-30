using System;
using System.Collections.Generic;

using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Data;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class SetWeatherCommand : Command
    {
        public override string Name => "setweather";
        public override string Description => "Set World Weather.";
        public override IEnumerable<string> Aliases => new string[] { "sw" };
        public override PermissionFlags Permissions => PermissionFlags.ModeratorOrHigher;

        public SetWeatherCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                if (Enum.TryParse(arguments[0], true, out Weather weather))
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

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <Weather>");
    }
}