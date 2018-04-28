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

        public override bool MessageSend(ChatMessage chatMessage) => base.MessageSend(chatMessage) && Script.MessageSend(chatMessage);

        public override bool Subscribe(Client client) => base.Subscribe(client) && Script.Subscribe(client);

        public override bool UnSubscribe(Client client) => base.UnSubscribe(client) && Script.UnSubscribe(client);
    }
}