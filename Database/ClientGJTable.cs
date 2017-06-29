using SQLite;

namespace PokeD.Server.Database
{
    public sealed class ClientGJTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientID { get; private set; }

        public long GameJoltID { get; private set; }


        public ClientGJTable() { }
        public ClientGJTable(int clientID, long gameJoltID)
        {
            ClientID = clientID;

            GameJoltID = gameJoltID;
        }
    }
}