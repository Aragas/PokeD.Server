using System;

using Aragas.Core.Wrappers;

using PokeD.Server.Data;

namespace PokeD.Server.DatabaseData
{
    public sealed class Trade : DatabaseTable<Guid>
    {
        public override Guid Id { get; protected set; } = Guid.NewGuid();


        public Guid TradePlayerID_0 { get; private set; }
        public Guid TradePlayerID_1 { get; private set; }


        public Trade() { }
        public Trade(Database database, TradeInstance trade)
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
