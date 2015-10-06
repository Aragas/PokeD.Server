using System;

namespace PokeD.Server.Statistics.Data
{
    public struct Trade
    {
        public enum TradeType
        {
            WonderTrade,
            Trade
        }

        public TradeType Type { get; set; }
        public ulong NumberOfTrades { get; set; }
        public TimeSpan DurationOfVisit { get; set; }
    }
}