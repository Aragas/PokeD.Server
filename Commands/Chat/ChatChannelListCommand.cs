using System.Collections.Generic;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public class ChatChannelListCommand : Command
    {
        public override string Name { get; protected set; } = "chatchannellist";
        public override string Description { get; protected set; } = "Get all Chat Channels.";
        public override IEnumerable<string> Aliases { get; protected set; } = new [] { "channellist", "chatl", "chanl", "cl" };
        public override PermissionFlags Permissions { get; protected set; } = PermissionFlags.VerifiedOrHigher;

        public ChatChannelListCommand(Server server) : base(server) { }

        public override void Handle(Client client, string alias, string[] arguments)
        {
            if (arguments.Length == 1)
            {
                var channelName = arguments[0].ToLower();
                var channel = ChatChannelManager.FindByAlias(channelName);
                client.SendServerMessage(channel != null ? $"{channel.Name}: {channel.Description}" : $"Channel '{channelName}' not found!");
            }
            else
                client.SendServerMessage($"Invalid arguments given.");
        }

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <global/local/'custom'>"); }
    }
}