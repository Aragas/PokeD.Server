using System;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class TradeTable : IdatabaseTable
    {
        [PrimaryKey]
        public Guid Id { get; private set; } = Guid.NewGuid();


        public Guid TradeClient0Id { get; private set; }
        public Guid TradeClient1Id { get; private set; }


        public TradeTable() { }
        public TradeTable(SQLiteConnection database, TradeInstance trade)
        {
            var tradeClient0 = new TradeClient(database, trade.Client0Id, trade.Client0Monster);
            database.Insert(tradeClient0);
            TradeClient0Id = tradeClient0.Id;

            var tradeClient1 = new TradeClient(database, trade.Client1Id, trade.Client1Monster);
            database.Insert(tradeClient1);
            TradeClient1Id = tradeClient1.Id;
        }
    }
}