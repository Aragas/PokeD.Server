using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public class LocalChatChannel : ChatChannel
    {
        public override string Name => "Local Chat";
        public override string Description => "Local Chat System.";
        public override string Alias => "local";

        public Dictionary<string, List<Client>> ClientsInLocation { get; } = new Dictionary<string, List<Client>>();
        public Dictionary<Client, string> ClientInLocation { get; } = new Dictionary<Client, string>();


        public override bool MessageSend(ChatMessage chatMessage)
        {
            if (!ClientInLocation.ContainsKey(chatMessage.Sender))
                return false;

            var sender = chatMessage.Sender;
            var location = sender.LevelFile;
            CheckLocation(sender);

            foreach (var client in ClientsInLocation[location])
                client.SendChatMessage(chatMessage);

            return true;
        }

        private void CheckLocation(Client client)
        {
            var location = client.LevelFile;
            if (location != ClientInLocation[client])
            {
                var oldLocation = ClientInLocation[client];

                ClientInLocation[client] = location;

                if (!ClientsInLocation.ContainsKey(location))
                    ClientsInLocation.Add(location, new List<Client>());
                ClientsInLocation[location].Add(client);

                if (ClientsInLocation[oldLocation].Contains(client))
                    ClientsInLocation[oldLocation].Remove(client);
            }
        }

        public override bool Subscribe(Client client)
        {
            if (ClientInLocation.ContainsKey(client))
                CheckLocation(client);
            else
            {
                ClientInLocation.Add(client, client.LevelFile);

                if (!ClientsInLocation.ContainsKey(client.LevelFile))
                    ClientsInLocation.Add(client.LevelFile, new List<Client>());
                ClientsInLocation[client.LevelFile].Add(client);
            }


            return true;
        }

        public override bool UnSubscribe(Client client)
        {
            if (!ClientInLocation.ContainsKey(client))
                return false;

            var oldLocation = ClientInLocation[client];
            var location = client.LevelFile;


            if (oldLocation != location)
            {
                if (ClientsInLocation.ContainsKey(oldLocation) && ClientsInLocation[oldLocation].Contains(client))
                    ClientsInLocation[oldLocation].Remove(client);
            }
            if (ClientsInLocation.ContainsKey(location) && ClientsInLocation[location].Contains(client))
                ClientsInLocation[location].Remove(client);

            ClientInLocation.Remove(client);


            return true;
        }
    }
}