using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public class ChatMessage
    {
        public Client Sender { get; }
        public string Message { get; }

        public ChatMessage(Client sender, string message) { Sender = sender; Message = message; }
    }
}