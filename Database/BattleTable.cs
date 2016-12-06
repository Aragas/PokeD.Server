/*
using System;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class BattleTable : IdatabaseTable
    {
        [PrimaryKey]
        public Guid Id { get; private set; } = Guid.NewGuid();


        public string PlayerIds { get; private set; }


        public BattleTable() { }
        //public BattleTable(BattleInstance battle)
        //{
        //    Id = battle.BattleId;
        //
        //    PlayerIds = string.Join(",", battle.Trainers.Ids);
        //}
    }
}
*/