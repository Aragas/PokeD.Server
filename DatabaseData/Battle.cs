using System;

using Aragas.Core.Wrappers;

using PokeD.Server.Clients.PokeD;

namespace PokeD.Server.DatabaseData
{
    public sealed class Battle : DatabaseTable<Guid>
    {
        public override Guid Id { get; protected set; } = Guid.NewGuid();


        public string PlayerIDs { get; private set; }


        public Battle() { }
        public Battle(BattleInstance battle)
        {
            Id = battle.BattleID;

            PlayerIDs = string.Join(",", battle.Trainers.IDs);
        }
    }
}
