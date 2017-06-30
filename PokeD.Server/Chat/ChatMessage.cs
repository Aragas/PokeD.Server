using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public class ChatChannelMessage
    {
        public ChatChannel ChatChannel { get; }
        public ChatMessage ChatMessage { get; }

        public ChatChannelMessage(ChatChannel chatChannel, ChatMessage chatMessage) { ChatChannel = chatChannel; ChatMessage = chatMessage; }
    }

    public class ChatMessage
    {
        public Client Sender { get; }
        public string Message { get; }

        public ChatMessage(Client sender, string message) { Sender = sender; Message = message; }
    }
}