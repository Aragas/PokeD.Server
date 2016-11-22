using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Commands
{
    public class ChangeChatChannelCommand : Command
    {
        public override string Name { get; protected set; } = "changechatchannel";
        public override string Description { get; protected set; } = "Change Client chat channel permission.";
        public override IEnumerable<string> Aliases { get; protected set; } = new string[] { "changechannel", "cchat", "cchan", "cc" };

        public ChangeChatChannelCommand(Server server) : base(server) { }

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

        public override void Help(Client client, string alias){ client.SendServerMessage($"Correct usage is /{alias} <global/local/custom>"); }
    }
}