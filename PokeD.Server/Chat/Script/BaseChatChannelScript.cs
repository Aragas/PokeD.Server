using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public abstract class BaseChatChannelScript
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Aliases { get; }

        public abstract bool SendMessage(ChatMessage chatMessage);

        public abstract bool Subscribe(Client client);

        public abstract bool UnSubscribe(Client client);
    }
}