using System;
using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class ChatChannelInfoCommand : Command
    {
        public override string Name => "chatchannelinfo";
        public override string Description => "Get Chat Channel Info.";
        public override IEnumerable<string> Aliases => new[] { "channelinfo", "chati", "chani", "ci" };
        public override PermissionFlags Permissions => PermissionFlags.UserOrHigher;

        public ChatChannelInfoCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                var channelName = arguments[0].ToLower();
                var channel = ChatChannelManager.FindByAlias(channelName);
                client.SendServerMessage(channel != null ? $"{channel.Name}: {channel.Description}" : $"Channel '{channelName}' not found!");
            }
            else
                client.SendServerMessage("Invalid arguments given.");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias} <global/local/'custom'>");
    }
}