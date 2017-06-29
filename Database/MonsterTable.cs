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
        public int? MonsterID { get; private set; }


        public short Species { get; private set; }

        #region CatchInfo
        public string Method { get; private set; } = string.Empty;
        public string Location { get; private set; } = string.Empty;

        public string TrainerName { get; private set; } = string.Empty;
        public ushort TrainerID { get; private set; }

        public byte PokeballID { get; private set; }

        public string Nickname { get; private set; } = string.Empty;
        #endregion

        public uint PersonalityValue { get; private set; }

        public Gender Gender { get; private set; }

        public short Ability { get; private set; }
        public byte Nature { get; private set; }

        public long Experience { get; private set; }

        public string IV_Json { get; private set; } = string.Empty;
        public string EV_Json { get; private set; } = string.Empty;

        public short CurrentHP { get; private set; }
        public byte StatusEffect { get; private set; }

        public byte Affection { get; private set; }
        public byte Friendship { get; private set; }

        public bool IsShiny { get; private set; }

        public int EggSteps { get; private set; }


        public string Moves_Json { get; private set; } = string.Empty;

        public int HeldItem { get; private set; }
        

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

            IV_Json = JsonConvert.SerializeObject(monster.IV, Formatting.None, new StatsConverter());
            EV_Json = JsonConvert.SerializeObject(monster.EV, Formatting.None, new StatsConverter());

            CurrentHP = monster.CurrentHP;
            StatusEffect = monster.StatusEffect;

            Affection = monster.Affection;
            Friendship = monster.Friendship;

            IsShiny = monster.IsShiny;

            EggSteps = monster.EggSteps;

            Moves_Json = JsonConvert.SerializeObject(monster.Moves, Formatting.None, new AttackConverter());

            HeldItem = monster.HeldItem?.StaticData?.ID ?? 0;
        }
    }
}