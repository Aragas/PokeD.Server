using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public class ScriptChatChannel : ChatChannel
    {
        private BaseChatChannelScript Script { get; }

        public override string Name => Script.Name;
        public override string Description => Script.Description;
        public override string Alias => Script.Aliases;

        public ScriptChatChannel(BaseChatChannelScript script) => Script = script;

        public override bool MessageSend(ChatMessage chatMessage)
        {
            if (!base.MessageSend(chatMessage))
                return false;

            return Script.MessageSend(chatMessage);
        }

        public override bool Subscribe(Client client)
        {
            if (!base.Subscribe(client))
                return false;

            return Script.Subscribe(client);
        }

        public override bool UnSubscribe(Client client)
        {
            if (!base.UnSubscribe(client))
                return false;

            return Script.UnSubscribe(client);
        }
    }
}