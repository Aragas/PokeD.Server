using System;

using PokeD.Core.Data.PokeD;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class TradeClient : IDatabaseTable
    {
        [PrimaryKey]
        public Guid ID { get; private set; } = Guid.NewGuid();


        public int ClientID { get; private set; }

        public Guid MonsterID { get; private set; }


        public TradeClient() { }
        public TradeClient(SQLiteConnection database, int playerID, Monster monster)
        {
            ClientID = playerID;

            var mon = new MonsterDB(monster);
            database.Insert(mon);
            MonsterID = mon.ID;
        }
    }
}