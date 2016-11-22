using System;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Data.PokeD.Monster.Data;

using SQLite.Net.Attributes;

namespace PokeD.Server.Database
{
    public sealed class MonsterDB : IDatabaseTable
    {
        private static string GetString(MonsterStats monsterStats)
        {
            return string.Join(",",
                monsterStats.Attack, monsterStats.Defense, monsterStats.SpecialAttack, monsterStats.SpecialDefense, monsterStats.Speed);
        }
        private static MonsterStats GetMonsterStats(string text)
        {
            var array = text.Split(',');
            return new MonsterStats(
                short.Parse(array[0]), short.Parse(array[1]), short.Parse(array[2]), short.Parse(array[3]), short.Parse(array[4]), short.Parse(array[5]));
        }

        private static string GetString(MonsterMoves monsterMoves)
        {
            return string.Empty;
            //return string.Join(",", monsterMove.ID, monsterMove.PPUPs);
        }
        private static MonsterMoves GetMonsterMoves(string text)
        {
            return MonsterMoves.Empty;
            //var array = text.Split(',');
            //return new MonsterMove(int.Parse(array[0]), int.Parse(array[1]));
        }



        [PrimaryKey]
        public Guid Id { get; protected set; } = Guid.NewGuid();


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

        public int Experience { get; private set; }

        public int EggSteps { get; private set; }

        public string IV { get; private set; } = string.Empty;
        public string EV { get; private set; } = string.Empty;
        public string HiddenEV { get; private set; } = string.Empty;

        public short CurrentHP { get; private set; }
        public short StatusEffect { get; private set; }

        public byte Affection { get; private set; }
        public byte Friendship { get; private set; }

        public string Moves { get; private set; } = string.Empty;

        public short HeldItem { get; private set; }
        

        public MonsterDB() { }
        public MonsterDB(Monster monster)
        {
            Species = monster.Species;
            SecretID = monster.InstanceData.SecretID;

            Method = monster.CatchInfo.Method;
            Location = monster.CatchInfo.Location;
            TrainerName = monster.CatchInfo.TrainerName;
            TrainerID = monster.CatchInfo.TrainerID;
            PokeballID = monster.CatchInfo.PokeballID;
            Nickname = monster.CatchInfo.Nickname;

            PersonalityValue = monster.InstanceData.PersonalityValue;

            Nature = monster.Nature;

            Experience = monster.Experience;

            EggSteps = monster.EggSteps;

            IV = GetString(monster.InstanceData.IV);
            EV = GetString(monster.InstanceData.EV);
            HiddenEV = GetString(monster.InstanceData.HiddenEV);

            CurrentHP = monster.CurrentHP;
            StatusEffect = monster.StatusEffect;

            Affection = monster.Affection;
            Friendship = monster.Friendship;

            Moves = GetString(monster.Moves);

            HeldItem = monster.HeldItem;
        }
    }
}