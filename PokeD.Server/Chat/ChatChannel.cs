using System.Collections.Generic;

using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Chat
{
    public abstract class ChatChannel
    {
        private static readonly object _dictionaryLock = new object();
        private static readonly Dictionary<Client, ChatChannel> _subscription = new Dictionary<Client, ChatChannel>();
        public static Dictionary<Client, ChatChannel> Subscription { get { lock(_dictionaryLock) { return _subscription; } } }

        public ReaderWriterLockList<Client> Subscribers { get; } = new ReaderWriterLockList<Client>();
        public List<ChatMessage> History { get; } = new List<ChatMessage>();

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Alias { get; }

        public virtual bool MessageSend(ChatMessage chatMessage)
        {
            if (!Subscription.ContainsKey(chatMessage.Sender) || Subscription[chatMessage.Sender] != this)
                return false;

            History.Add(chatMessage);

            for (var i = Subscribers.Count - 1; i >= 0; i--)
                Subscribers[i].SendChatMessage(new ChatChannelMessage(this, chatMessage));

            return true;
        }

        public virtual bool Subscribe(Client client)
        {
            if (Subscription.ContainsKey(client) && Subscription[client] != null)
                Subscription[client].UnSubscribe(client);
            
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
            if (!Subscription.ContainsKey(client) ||  Subscription[client] != this)
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