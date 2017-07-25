using System.Collections.Generic;

using PokeD.Core.Services;
using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class ChatChannelListCommand : Command
    {
        public override string Name => "chatchannellist";
        public override string Description => "Get all Chat Channels.";
        public override IEnumerable<string> Aliases => new [] { "channellist", "chatl", "chanl", "cl" };
        public override PermissionFlags Permissions => PermissionFlags.UserOrHigher;

        public ChatChannelListCommand(IServiceContainer componentManager) : base(componentManager) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            foreach (var channel in ChatChannelManager.GetChatChannels())
                client.SendServerMessage($"{channel.Name}: {channel.Description}");
        }

        public override void Help(Client client, string alias) => client.SendServerMessage($"Correct usage is /{alias}");
    }
}