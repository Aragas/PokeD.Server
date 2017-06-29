using SQLite;

namespace PokeD.Server.Database
{
    public sealed class ClientChannelTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientID { get; private set; }

        public int Channel { get; private set; }


        public ClientChannelTable() { }
        public ClientChannelTable(int clientID, int channel)
        {
            ClientID = clientID;

            Channel = channel;
        }
    }
}