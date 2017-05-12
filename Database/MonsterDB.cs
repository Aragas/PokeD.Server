using System;
using System.Collections.Generic;

using PokeD.BattleEngine.Attack;
using PokeD.BattleEngine.Item;
using PokeD.BattleEngine.Monster.Data;
using PokeD.Core.Data.PokeD;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class MonsterDB : IDatabaseTable
    {
        private static string GetString(Stats monsterStats)
        {
            return string.Join(",",
                monsterStats.Attack, monsterStats.Defense, monsterStats.SpecialAttack, monsterStats.SpecialDefense, monsterStats.Speed);
        }
        private static Stats GetStats(string text)
        {
            var array = text.Split(',');
            return new Stats(
                short.Parse(array[0]), short.Parse(array[1]), short.Parse(array[2]), short.Parse(array[3]), short.Parse(array[4]), short.Parse(array[5]));
        }

        private static string GetString(IList<BaseAttackInstance> attacks)
        {
            string str = string.Empty;
            for (int i = 0; i < 4; i++)
            {
                if (attacks.Count > i)
                {
                    var attack = attacks[i];
                    str += $"|{attack.StaticData.ID},{attack.PPCurrent},{attack.PPUps}";
                }
            }
            str += "|";

            return str;
            //return string.Join(",", monsterMove.ID, monsterMove.PPUPs);
        }
        private static IList<BaseAttackInstance> GetAttacks(string text)
        {
            var list = new List<BaseAttackInstance>();
            return list;
            //return MonsterMoves.Empty;
            //var array = text.Split(',');
            //return new MonsterMove(int.Parse(array[0]), int.Parse(array[1]));
        }



        [PrimaryKey]
        public Guid ID { get; private set; } = Guid.NewGuid();


        public short Species { get; private set; }
        public ushort SecretID { get; private set; }

        #region CatchInfo
        public string Method { get; private set; } = string.Empty;
        public string Location { get; private set; } = string.Empty;

        public string TrainerName { get; private set; } = string.Empty;
        public ushort TrainerID { get; private set; }

        public byte PokeballID { get; private set; }

        public string Nickname { get; private set; } = string.Empty;
        #endregion

        public uint PersonalityValue { get; private set; }

        public byte Nature { get; private set; }

        public long Experience { get; private set; }

        public int EggSteps { get; private set; }

        public string IV { get; private set; } = string.Empty;
        public string EV { get; private set; } = string.Empty;

        public short CurrentHP { get; private set; }
        public short StatusEffect { get; private set; }

        public byte Affection { get; private set; }
        public byte Friendship { get; private set; }

        public string Moves { get; private set; } = string.Empty;

        public BaseItemInstance HeldItem { get; private set; }
        

        public MonsterDB() { }
        public MonsterDB(Monster monster)
        {
            Species = monster.StaticData.ID;
            SecretID = monster.SecretID;

            Method = monster.CatchInfo.Method;
            Location = monster.CatchInfo.Location;
            TrainerName = monster.CatchInfo.TrainerName;
            TrainerID = monster.CatchInfo.TrainerID;
            PokeballID = monster.CatchInfo.PokeballID;
            Nickname = monster.CatchInfo.Nickname;

            PersonalityValue = monster.PersonalityValue;

            Nature = monster.Nature;

            Experience = monster.Experience;

            EggSteps = monster.EggSteps;

            IV = GetString(monster.IV);
            EV = GetString(monster.EV);

            CurrentHP = monster.CurrentHP;
            StatusEffect = monster.StatusEffect;

            Affection = monster.Affection;
            Friendship = monster.Friendship;

            Moves = GetString(monster.Moves);

            HeldItem = monster.HeldItem;
        }
    }
}