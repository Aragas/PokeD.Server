using SQLite;

namespace PokeD.Server.Database
{
    public sealed class ClientGJTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientID { get; set; }

        public long GameJoltID { get; set; }


        public ClientGJTable() { }
        public ClientGJTable(int clientID, long gameJoltID)
        {
            ClientID = clientID;

            GameJoltID = gameJoltID;
        }
    }
}