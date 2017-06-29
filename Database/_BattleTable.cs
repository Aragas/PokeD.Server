/*
using System;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class BattleTable : IDatabaseTable
    {
        [PrimaryKey]
        public Guid ID { get; private set; } = Guid.NewGuid();


        public string PlayerIDs { get; private set; }


        public BattleTable() { }
        //public BattleTable(BattleInstance battle)
        //{
        //    ID = battle.BattleID;
        //
        //    PlayerIDs = string.Join(",", battle.Trainers.IDs);
        //}
    }
}
*/