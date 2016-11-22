using SQLite.Net.Attributes;

namespace PokeD.Server.Database
{
    public sealed class ClientChannelTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientId { get; private set; }

        public int Channel { get; private set; }


        public ClientChannelTable() { }
        public ClientChannelTable(int clientId, int channel)
        {
            if (clientId >= 0)
                ClientId = clientId;

            Channel = channel;
        }
    }
}