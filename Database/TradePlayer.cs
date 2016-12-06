using System;

using PokeD.Core.Data.PokeD.Monster;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class TradeClient : IdatabaseTable
    {
        [PrimaryKey]
        public Guid Id { get; private set; } = Guid.NewGuid();


        public int ClientId { get; private set; }

        public Guid MonsterId { get; private set; }


        public TradeClient() { }
        public TradeClient(SQLiteConnection database, int playerId, Monster monster)
        {
            ClientId = playerId;

            var mon = new MonsterDB(monster);
            database.Insert(mon);
            MonsterId = mon.Id;
        }
    }
}