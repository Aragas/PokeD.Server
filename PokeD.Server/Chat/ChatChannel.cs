using System.Collections.Concurrent;
using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public abstract class ChatChannel
    {
        private static ConcurrentDictionary<Client, ChatChannel> Subscription { get; } = new ConcurrentDictionary<Client, ChatChannel>();

        public List<ChatMessage> History { get; } = new List<ChatMessage>();

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Alias { get; }

        public virtual bool MessageSend(ChatMessage chatMessage)
        {
            if (!Subscription.TryGetValue(chatMessage.Sender, out var chatChannel) || chatChannel != this)
                return false;

            History.Add(chatMessage);

            return true;
        }

        public virtual bool Subscribe(Client client)
        {
            if (Subscription.TryGetValue(client, out var chatChannel))
                chatChannel.UnSubscribe(client);

            lock (Subscription)
                Subscription.AddOrUpdate(client, this, (_, cc) => cc);

            return true;
        }

        public virtual bool UnSubscribe(Client client)
        {
            lock (Subscription)
                Subscription.TryRemove(client, out _);
            
            return true;
        }
    }
}