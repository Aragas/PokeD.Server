/*
using System.Collections.Generic;

using PCLExt.Lua;

using PokeD.Core.Data.PokeD.Trainer;

namespace PokeD.Server.Data
{
    public class BattleDataTable
    {
        public Trainer NPC { get; }
        public Trainer Opponent { get; }

        public BattleDataTable(Trainer npc, Trainer opponent)
        {
            NPC = npc;
            Opponent = opponent;
        }
        public BattleDataTable(LuaTable table)
        {
            if (table == null)
                return;

            //NPC = new Trainer(table["npc"] as LuaTable);
            //Opponent = new Trainer(table["opponent"] as LuaTable);
        }

        public LuaTable ToLuaTable(LuaTable table)
        {
            //table["npc"] = NPC.ToDictionary();
            //table["opponent"] = Opponent.ToDictionary();

            return table;
        }
        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>
            {
                {"npc", new Dictionary<object, object>()},
                {"opponent", new Dictionary<object, object>()}
            };


            //dictionary["npc"] = NPC.ToDictionary();
            //dictionary["opponent"] = Opponent.ToDictionary();

            return dictionary;
        }
    }
}
*/