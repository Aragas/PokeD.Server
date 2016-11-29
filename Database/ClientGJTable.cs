using SQLite;

namespace PokeD.Server.Database
{
    public sealed class ClientGJTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientId { get; private set; }

        public int GameJoltId { get; private set; }


        public ClientGJTable() { }
        public ClientGJTable(int clientId, int gameJoltId)
        {
            if (clientId >= 0)
                ClientId = clientId;

            GameJoltId = gameJoltId;
        }
    }
}