using SQLite;

namespace PokeD.Server.Database
{
    public sealed class ClientGJTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientID { get; private set; }

        public int GameJoltID { get; private set; }


        public ClientGJTable() { }
        public ClientGJTable(int clientID, int gameJoltID)
        {
            if (clientID >= 0)
                ClientID = clientID;

            GameJoltID = gameJoltID;
        }
    }
}