using SQLite;

namespace PokeD.Server.Database
{
    public sealed class TradeClientTable : IDatabaseTable
    {
        [PrimaryKey, AutoIncrement]
        public int? TradeClientID { get; set; }


        public int ClientID { get; set; }

        public int MonsterID { get; set; }


        public TradeClientTable() { }
        public TradeClientTable(int clientID, int monsterID)
        {
            ClientID = clientID;

            MonsterID = monsterID;
        }
    }
}