using System;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class Trade : IDatabaseTable
    {
        [PrimaryKey]
        public Guid Id { get; private set; } = Guid.NewGuid();


        public Guid TradePlayerID_0 { get; private set; }
        public Guid TradePlayerID_1 { get; private set; }


        public Trade() { }
        public Trade(SQLiteConnection database, TradeInstance trade)
        {
            var tradePlayer0 = new TradePlayer(database, trade.Player_0_ID, trade.Player_0_Monster);
            database.Insert(tradePlayer0);
            TradePlayerID_0 = tradePlayer0.Id;

            var tradePlayer1 = new TradePlayer(database, trade.Player_1_ID, trade.Player_1_Monster);
            database.Insert(tradePlayer1);
            TradePlayerID_1 = tradePlayer1.Id;
        }
    }
}