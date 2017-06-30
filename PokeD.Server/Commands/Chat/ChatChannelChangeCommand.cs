using System.Collections.Generic;

using PokeD.Core.Services;
using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class ChatChannelChangeCommand : Command
    {
        public override string Name => "chatchannelchange";
        public override string Description => "Change Clients Chat Channel.";
        public override IEnumerable<string> Aliases => new [] { "channelchange", "chatc", "chanc", "cc" };
        public override PermissionFlags Permissions => PermissionFlags.UserOrHigher ^ PermissionFlags.Server;

        public ChatChannelChangeCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                var channelName = arguments[0].ToLower();
                var channel = ChatChannelManager.FindByAlias(channelName);
                if(channel != null)
                    client.SendServerMessage(channel.Subscribe(client) ? $"Changed chat channel to {channel.Name}!" : $"Failed to change chat channel to {channel.Name}!");
                else
                    client.SendServerMessage($"Channel '{channelName}' not found!");
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <global/local/'custom'>");
    }
}