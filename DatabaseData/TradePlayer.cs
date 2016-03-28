using System;

using Aragas.Core.Wrappers;

using PokeD.Core.Data.PokeD.Monster;

namespace PokeD.Server.DatabaseData
{
    public sealed class TradePlayer : DatabaseTable<Guid>
    {
        public override Guid Id { get; protected set; } = Guid.NewGuid();


        public int PlayerID { get; private set; }

        public Guid Monster_ID { get; private set; }


        public TradePlayer() { }
        public TradePlayer(Database database, int playerID, Monster monster)
        {
            PlayerID = playerID;

            var mon = new MonsterDB(monster);
            database.Insert(mon);
            Monster_ID = mon.Id;
        }
    }
}
