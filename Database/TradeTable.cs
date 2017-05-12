using System;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class TradeTable : IDatabaseTable
    {
        [PrimaryKey]
        public Guid ID { get; private set; } = Guid.NewGuid();


        public Guid TradeClient0ID { get; private set; }
        public Guid TradeClient1ID { get; private set; }


        public TradeTable() { }
        public TradeTable(SQLiteConnection database, TradeInstance trade)
        {
            var tradeClient0 = new TradeClient(database, trade.Client0ID, trade.Client0Monster);
            database.Insert(tradeClient0);
            TradeClient0ID = tradeClient0.ID;

            var tradeClient1 = new TradeClient(database, trade.Client1ID, trade.Client1Monster);
            database.Insert(tradeClient1);
            TradeClient1ID = tradeClient1.ID;
        }
    }
}