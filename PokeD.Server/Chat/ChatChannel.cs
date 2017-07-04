using System.Collections.Concurrent;
using System.Collections.Generic;

using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Chat
{
    public abstract class ChatChannel
    {
        protected static ConcurrentDictionary<Client, ChatChannel> Subscription { get; } = new ConcurrentDictionary<Client, ChatChannel>();

        public ReaderWriterLockList<Client> Subscribers { get; } = new ReaderWriterLockList<Client>();
        public List<ChatMessage> History { get; } = new List<ChatMessage>();

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Alias { get; }

        public virtual bool MessageSend(ChatMessage chatMessage)
        {
            if (!Subscription.TryGetValue(chatMessage.Sender, out _))
                return false;

            History.Add(chatMessage);

            for (var i = Subscribers.Count - 1; i >= 0; i--)
                Subscribers[i]?.SendChatMessage(new ChatChannelMessage(this, chatMessage));

            return true;
        }

        public virtual bool Subscribe(Client client)
        {
            if (Subscription.TryGetValue(client, out var chatChannel))
                chatChannel.UnSubscribe(client);
            
            if (!Subscribers.Contains(client))
            {
                Subscribers.Add(client);
                Subscription[client] = this;
                return true;
            }

            return false;
        }
        public virtual bool UnSubscribe(Client client)
        {
            if (!Subscription.TryGetValue(client, out _))
                return false;

            if (Subscribers.Contains(client))
            {
                Subscribers.Remove(client);
                Subscription[client] = null;
                return true;
            }
            
            return false;
        }
    }
}