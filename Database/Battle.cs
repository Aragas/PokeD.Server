using System;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class Battle : IDatabaseTable
    {
        [PrimaryKey]
        public Guid Id { get; private set; } = Guid.NewGuid();


        public string PlayerIDs { get; private set; }


        public Battle() { }
        //public Battle(BattleInstance battle)
        //{
        //    Id = battle.BattleID;
        //
        //    PlayerIDs = string.Join(",", battle.Trainers.IDs);
        //}
    }
}