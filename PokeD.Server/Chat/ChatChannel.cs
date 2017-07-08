using System.Collections.Concurrent;
using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public abstract class ChatChannel
    {
        protected static ConcurrentDictionary<Client, ChatChannel> Subscription { get; } = new ConcurrentDictionary<Client, ChatChannel>();

        public List<Client> Subscribers { get; } = new List<Client>();
        public List<ChatMessage> History { get; } = new List<ChatMessage>();

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Alias { get; }

        public virtual bool MessageSend(ChatMessage chatMessage)
        {
            if (!Subscription.TryGetValue(chatMessage.Sender, out _))
                return false;

            History.Add(chatMessage);

            lock (Subscribers)
            {
                foreach (var client in Subscribers)
                    client?.SendChatMessage(new ChatChannelMessage(this, chatMessage));
            }

            return true;
        }

        public virtual bool Subscribe(Client client)
        {
            if (Subscription.TryGetValue(client, out var chatChannel))
                chatChannel.UnSubscribe(client);

            lock (Subscribers)
            {
                if (!Subscribers.Contains(client))
                {
                    Subscribers.Add(client);
                    Subscription[client] = this;
                    return true;
                }
            }

            return false;
        }
        public virtual bool UnSubscribe(Client client)
        {
            if (!Subscription.TryGetValue(client, out _))
                return false;

            lock (Subscribers)
            {
                if (Subscribers.Contains(client))
                {
                    Subscribers.Remove(client);
                    Subscription[client] = null;
                    return true;
                }
            }

            return false;
        }
    }
}