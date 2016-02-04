using System;
using System.Collections.Generic;
using System.Linq;

using Aragas.Core.Extensions;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Data.PokeD.Monster.Data;

namespace PokeD.Server.Extensions
{
    public static class DataItemsExtensions
    {
        public static Monster[] DataItemsToMonsters(this DataItems data) => data.ToString().Split('|').Select(str => new DataItems(str)).Select(items => items.ToMonster()).ToArray();

        public static Monster ToMonster(this DataItems data)
        {
            var dict = data.ToDictionary();

            var id = Int16.Parse(dict["Pokemon"]);
            var gender = (MonsterGender)Int32.Parse(dict["Gender"]);
            var isShiny = Int32.Parse(dict["isShiny"]) != 0;
            var ability = Int16.Parse(dict["Ability"]);
            var nature = Byte.Parse(dict["Nature"]);

            var dat = new MonsterInstanceData(id, gender, isShiny, ability, nature)
            {
                Experience = Int32.Parse(dict["Experience"]),
                Friendship = Byte.Parse(dict["Friendship"]),
                CatchInfo = new MonsterCatchInfo()
                {
                    Nickname = String.IsNullOrEmpty(dict["NickName"]) ? String.Empty : dict["NickName"],
                    PokeballID = Byte.Parse(dict["CatchBall"]),
                    Method = dict["CatchMethod"],
                    Location = dict["CatchLocation"],
                    TrainerName = dict["CatchTrainer"],
                    TrainerID = (ushort)Int32.Parse(dict["OT"]).BitsGet(0, 16) == UInt16.MaxValue
                                ? (ushort)Int32.Parse(dict["OT"]).BitsGet(16, 32)
                                : (ushort)Int32.Parse(dict["OT"]).BitsGet(0, 16)
                }
            };

            dat.HeldItem = Int16.Parse(dict["Item"]);

            var move0 = dict["Attack1"].Split(',');
            var move1 = dict["Attack2"].Split(',');
            var move2 = dict["Attack3"].Split(',');
            var move3 = dict["Attack4"].Split(',');
            dat.Moves = new MonsterMoves(
                move0.Length != 1 ? new Move(Int32.Parse(move0[0]), Int32.Parse(move0[2]) - Int32.Parse(move0[1])) : Move.Empty,
                move1.Length != 1 ? new Move(Int32.Parse(move1[0]), Int32.Parse(move1[2]) - Int32.Parse(move1[1])) : Move.Empty,
                move2.Length != 1 ? new Move(Int32.Parse(move2[0]), Int32.Parse(move2[2]) - Int32.Parse(move2[1])) : Move.Empty,
                move3.Length != 1 ? new Move(Int32.Parse(move3[0]), Int32.Parse(move3[2]) - Int32.Parse(move3[1])) : Move.Empty);

            dat.CurrentHP = Int16.Parse(dict["HP"]);

            var ev = dict["EVs"].Split(',');
            var ev0 = (short)(Int16.Parse(ev[0]) > 1 ? Int16.Parse(ev[0]) - 1 : Int16.Parse(ev[0]));
            var ev1 = (short)(Int16.Parse(ev[1]) > 1 ? Int16.Parse(ev[1]) - 1 : Int16.Parse(ev[1]));
            var ev2 = (short)(Int16.Parse(ev[2]) > 1 ? Int16.Parse(ev[2]) - 1 : Int16.Parse(ev[2]));
            var ev3 = (short)(Int16.Parse(ev[3]) > 1 ? Int16.Parse(ev[3]) - 1 : Int16.Parse(ev[3]));
            var ev4 = (short)(Int16.Parse(ev[4]) > 1 ? Int16.Parse(ev[4]) - 1 : Int16.Parse(ev[4]));
            var ev5 = (short)(Int16.Parse(ev[5]) > 1 ? Int16.Parse(ev[5]) - 1 : Int16.Parse(ev[5]));
            dat.EV = new MonsterStats(ev0, ev1, ev2, ev3, ev4, ev5);

            var iv = dict["IVs"].Split(',');
            var iv0 = (short)(Int16.Parse(iv[0]) > 1 ? Int16.Parse(iv[0]) - 1 : Int16.Parse(iv[0]));
            var iv1 = (short)(Int16.Parse(iv[1]) > 1 ? Int16.Parse(iv[1]) - 1 : Int16.Parse(iv[1]));
            var iv2 = (short)(Int16.Parse(iv[2]) > 1 ? Int16.Parse(iv[2]) - 1 : Int16.Parse(iv[2]));
            var iv3 = (short)(Int16.Parse(iv[3]) > 1 ? Int16.Parse(iv[3]) - 1 : Int16.Parse(iv[3]));
            var iv4 = (short)(Int16.Parse(iv[4]) > 1 ? Int16.Parse(iv[4]) - 1 : Int16.Parse(iv[4]));
            var iv5 = (short)(Int16.Parse(iv[5]) > 1 ? Int16.Parse(iv[5]) - 1 : Int16.Parse(iv[5]));
            dat.IV = new MonsterStats(iv0, iv1, iv2, iv3, iv4, iv5);

            return new Monster(dat);
        }

        public static Dictionary<string, string> ToDictionary(this DataItems data)
        {
            var dict = new Dictionary<string, string>();
            var str = data.ToString();
            str = str.Replace("{", "");
            //str = str.Replace("}", ",");
            var array = str.Split('}');
            foreach (var s in array.Reverse().Skip(1))
            {
                var v = s.Split('"');
                dict.Add(v[1], v[2].Replace("[", "").Replace("]", ""));
            }

            return dict;
        }
    }
}
