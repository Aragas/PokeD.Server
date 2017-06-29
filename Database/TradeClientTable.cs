using SQLite;

namespace PokeD.Server.Database
{
    public sealed class TradeClientTable : IDatabaseTable
    {
        [PrimaryKey, AutoIncrement]
        public int? TradeClientID { get; private set; }


        public int ClientID { get; private set; }

        public int MonsterID { get; private set; }


        public TradeClientTable() { }
        public TradeClientTable(int clientID, int monsterID)
        {
            ClientID = clientID;

            MonsterID = monsterID;
        }
    }
}