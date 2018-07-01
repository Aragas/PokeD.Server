using System;
using Newtonsoft.Json;

using PokeD.BattleEngine.Monster.Enums;
using PokeD.Core.Data.PokeD;
using PokeD.Server.Database.JsonConverters;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class MonsterTable : IDatabaseTable
    {
        [PrimaryKey, AutoIncrement]
        public int? MonsterID { get; set; }


        public short Species { get; set; }

        #region CatchInfo
        public string Method { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public string TrainerName { get; set; } = string.Empty;
        public ushort TrainerID { get; set; }

        public byte PokeballID { get; set; }

        public string Nickname { get; set; } = string.Empty;
        #endregion

        public uint PersonalityValue { get; set; }

        public Gender Gender { get; set; }

        public short Ability { get; set; }
        public byte Nature { get; set; }

        public long Experience { get; set; }

        public string IV_Json { get; set; } = string.Empty;
        public string EV_Json { get; set; } = string.Empty;

        public short CurrentHP { get; set; }
        public byte StatusEffect { get; set; }

        public byte Affection { get; set; }
        public byte Friendship { get; set; }

        public bool IsShiny { get; set; }

        public int EggSteps { get; set; }


        public string Moves_Json { get; set; } = string.Empty;

        public int HeldItem { get; set; }
        

        public MonsterTable() { }
        public MonsterTable(Monster monster)
        {
            Species = monster.StaticData.ID;

            Method = monster.CatchInfo.Method;
            Location = monster.CatchInfo.Location;
            TrainerName = monster.CatchInfo.TrainerName;
            TrainerID = monster.CatchInfo.TrainerID;
            PokeballID = monster.CatchInfo.PokeballID;
            Nickname = monster.CatchInfo.Nickname;

            PersonalityValue = monster.PersonalityValue;

            Gender = monster.Gender;

            Ability = monster.Ability.StaticData.ID;
            Nature = monster.Nature;

            Experience = monster.Experience;

            IV_Json = JsonConvert.SerializeObject(monster.IV, Formatting.None, new StatsConverter()) ?? string.Empty;
            EV_Json = JsonConvert.SerializeObject(monster.EV, Formatting.None, new StatsConverter()) ?? string.Empty;

            CurrentHP = monster.CurrentHP;
            StatusEffect = monster.StatusEffect;

            Affection = monster.Affection;
            Friendship = monster.Friendship;

            IsShiny = monster.IsShiny;

            EggSteps = monster.EggSteps;

            Moves_Json = JsonConvert.SerializeObject(monster.Moves, Formatting.None, new AttackConverter()) ?? string.Empty;

            HeldItem = monster.HeldItem?.StaticData?.ID ?? 0;
        }
    }
}