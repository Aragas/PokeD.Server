using System.Collections.Generic;
using System.Linq;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Data
{
    public class Pokemon
    {
        public enum Move { }
        public enum Ability { }
        public enum Gender { Male, Female, None }

        public Move[] Moves { get; }
        public Ability[] Abilities { get; }
        public Gender GenderEnum { get; }
        public int Number { get; }

        public Pokemon(Move[] moves, Ability[] abilities, Gender gender, int number)
        {
            Moves = moves;
            Abilities = abilities;
            GenderEnum = gender;
            Number = number;
        }
        public Pokemon(ILuaTable table)
        {
            if (table == null)
                return;

            Moves = (table["moves"] as ILuaTable)?.ToList().Select(i => (Move) (int) (double) i).ToArray();
            Abilities = (table["abilities"] as ILuaTable)?.ToList().Select(i => (Ability) (int) (double) i).ToArray();
            GenderEnum = (Gender) (int) (double) table["gender"];
            Number = (int) (double) table["number"];
        }

        public ILuaTable ToLuaTable(ILuaTable table)
        {
            table["moves"] = Moves.Select(i => (double) (int) i).ToArray();
            table["abilities"] = Abilities.Select(i => (double) (int) i).ToArray();
            table["gender"] = (double) (int) GenderEnum;
            table["number"] = (double) Number;

            return table;
        }
        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>
            {
                ["moves"] = Moves.Select(i => (double) (int) i).ToArray(),
                ["abilities"] = Abilities.Select(i => (double) (int) i).ToArray(),
                ["gender"] = (double) (int) GenderEnum,
                ["number"] = (double) Number
            };


            return dictionary;
        }
    }

    public class Trainer
    {
        public enum TrainerType { }

        public TrainerType Type { get; }
        public Pokemon Poke { get; }

        public Trainer(TrainerType type, Pokemon pokemon)
        {
            Type = type;
            Poke = pokemon;
        }
        public Trainer(ILuaTable table)
        {
            if (table == null)
                return;

            Type = (TrainerType) (int) (double) table["type"];
            Poke = new Pokemon(table["pokemon"] as ILuaTable);
        }

        public ILuaTable ToLuaTable(ILuaTable table)
        {
            table["type"] = (double) (int) Type;
            table["pokemon"] = Poke.ToDictionary();

            return table;
        }
        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>
                {
                    {"pokemon", new Dictionary<object, object>()}
                };


            dictionary["type"] = (double) (int) Type;
            dictionary["pokemon"] = Poke.ToDictionary();

            return dictionary;
        }
    }

    public class BattleDataTable
    {
        public Trainer NPC { get; }
        public Trainer Opponent { get; }

        public BattleDataTable(Trainer npc, Trainer opponent)
        {
            NPC = npc;
            Opponent = opponent;
        }
        public BattleDataTable(ILuaTable table)
        {
            if (table == null)
                return;

            NPC = new Trainer(table["npc"] as ILuaTable);
            Opponent = new Trainer(table["opponent"] as ILuaTable);
        }

        public ILuaTable ToLuaTable(ILuaTable table)
        {
            table["npc"] = NPC.ToDictionary();
            table["opponent"] = Opponent.ToDictionary();

            return table;
        }
        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>
            {
                {"npc", new Dictionary<object, object>()},
                {"opponent", new Dictionary<object, object>()}
            };


            dictionary["npc"] = NPC.ToDictionary();
            dictionary["opponent"] = Opponent.ToDictionary();

            return dictionary;
        }
    }
}
