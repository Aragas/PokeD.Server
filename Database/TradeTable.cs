using PokeD.Server.Data;
using PokeD.Server.Services;
using SQLite;

namespace PokeD.Server.Database
{
    public sealed class TradeTable : IDatabaseTable
    {
        [PrimaryKey, AutoIncrement]
        public int? TradeID { get; private set; }


        public int TradeClient0ID { get; private set; }
        public int TradeClient1ID { get; private set; }


        public TradeTable() { }
        public TradeTable(int tradeClient0ID, int tradeClient1ID)
        {
            TradeClient0ID = tradeClient0ID;
            TradeClient1ID = tradeClient1ID;
        }
        public TradeTable(DatabaseService databaseService, TradeInstance tradeInstance)
        {
            var clientMonster0 = new MonsterTable(tradeInstance.Client0Monster);
            databaseService.DatabaseSet(clientMonster0);

            var tradeClient0 = new TradeClientTable(tradeInstance.Client0ID, clientMonster0.MonsterID.Value);
            databaseService.DatabaseSet(tradeClient0);
            TradeClient0ID = tradeClient0.TradeClientID.Value;


            var clientMonster1 = new MonsterTable(tradeInstance.Client1Monster);
            databaseService.DatabaseSet(clientMonster1);

            var tradeClient1 = new TradeClientTable(tradeInstance.Client1ID, clientMonster1.MonsterID.Value);
            databaseService.DatabaseSet(tradeClient1);
            TradeClient1ID = tradeClient1.TradeClientID.Value;
        }
    }
}