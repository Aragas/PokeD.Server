using System;

using PokeD.Core.Data.PokeD.Monster;

using SQLite.Net;
using SQLite.Net.Attributes;

namespace PokeD.Server.Database
{
    public sealed class TradePlayer : IDatabaseTable
    {
        [PrimaryKey]
        public Guid Id { get; protected set; } = Guid.NewGuid();


        public int PlayerID { get; private set; }

        public Guid Monster_ID { get; private set; }


        public TradePlayer() { }
        public TradePlayer(SQLiteConnection database, int playerID, Monster monster)
        {
            PlayerID = playerID;

            var mon = new MonsterDB(monster);
            database.Insert(mon);
            Monster_ID = mon.Id;
        }
    }
}