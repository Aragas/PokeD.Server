using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public class GlobalChatChannel : ChatChannel
    {
        public override string Name => "Global Chat";
        public override string Description => "Global Chat System.";
        public override string Alias => "global";


        public List<Client> Subscribers { get; } = new();

        public override bool SendMessage(ChatMessage chatMessage)
        {
            if (!base.SendMessage(chatMessage))
                return false;

            lock (Subscribers)
            {
                foreach (var client in Subscribers)
                    client?.SendChatMessage(this, chatMessage);
            }

            return true;
        }

        public override bool Subscribe(Client client)
        {
            if (!base.Subscribe(client))
                return false;

            lock (Subscribers)
            {
                if (!Subscribers.Contains(client))
                {
                    Subscribers.Add(client);
                    return true;
                }
            }

            return false;
        }
        public override bool Unsubscribe(Client client)
        {
            if (!base.Unsubscribe(client))
                return false;

            lock (Subscribers)
            {
                if (Subscribers.Contains(client))
                {
                    Subscribers.Remove(client);
                    return true;
                }
            }

            return false;
        }
    }
}