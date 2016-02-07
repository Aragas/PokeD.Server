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

            var id = short.Parse(dict["Pokemon"]);
            var gender = (MonsterGender) int.Parse(dict["Gender"]);
            var isShiny = int.Parse(dict["isShiny"]) != 0;
            var ability = short.Parse(dict["Ability"]);
            var nature = byte.Parse(dict["Nature"]);

            var dat = new MonsterInstanceData(id, gender, isShiny, ability, nature)
            {
                Experience = int.Parse(dict["Experience"]),
                Friendship = byte.Parse(dict["Friendship"]),
                CatchInfo = new MonsterCatchInfo()
                {
                    Nickname = string.IsNullOrEmpty(dict["NickName"]) ? string.Empty : dict["NickName"],
                    PokeballID = byte.Parse(dict["CatchBall"]),
                    Method = dict["CatchMethod"],
                    Location = dict["CatchLocation"],
                    TrainerName = dict["CatchTrainer"],
                    TrainerID = (ushort) int.Parse(dict["OT"]).BitsGet(0, 16) == ushort.MaxValue
                                ? (ushort) int.Parse(dict["OT"]).BitsGet(16, 32)
                                : (ushort) int.Parse(dict["OT"]).BitsGet(0, 16)
                }
            };

            dat.HeldItem = short.Parse(dict["Item"]);

            var move0 = dict["Attack1"].Split(',');
            var move1 = dict["Attack2"].Split(',');
            var move2 = dict["Attack3"].Split(',');
            var move3 = dict["Attack4"].Split(',');
            dat.Moves = new MonsterMoves(
                move0.Length != 1 ? new MonsterMove(int.Parse(move0[0]), int.Parse(move0[2]) - int.Parse(move0[1])) : MonsterMove.Empty,
                move1.Length != 1 ? new MonsterMove(int.Parse(move1[0]), int.Parse(move1[2]) - int.Parse(move1[1])) : MonsterMove.Empty,
                move2.Length != 1 ? new MonsterMove(int.Parse(move2[0]), int.Parse(move2[2]) - int.Parse(move2[1])) : MonsterMove.Empty,
                move3.Length != 1 ? new MonsterMove(int.Parse(move3[0]), int.Parse(move3[2]) - int.Parse(move3[1])) : MonsterMove.Empty);

            dat.CurrentHP = short.Parse(dict["HP"]);

            var ev = dict["EVs"].Split(',');
            var ev0 = (short) (short.Parse(ev[0]) > 1 ? short.Parse(ev[0]) - 1 : short.Parse(ev[0]));
            var ev1 = (short) (short.Parse(ev[1]) > 1 ? short.Parse(ev[1]) - 1 : short.Parse(ev[1]));
            var ev2 = (short) (short.Parse(ev[2]) > 1 ? short.Parse(ev[2]) - 1 : short.Parse(ev[2]));
            var ev3 = (short) (short.Parse(ev[3]) > 1 ? short.Parse(ev[3]) - 1 : short.Parse(ev[3]));
            var ev4 = (short) (short.Parse(ev[4]) > 1 ? short.Parse(ev[4]) - 1 : short.Parse(ev[4]));
            var ev5 = (short) (short.Parse(ev[5]) > 1 ? short.Parse(ev[5]) - 1 : short.Parse(ev[5]));
            dat.EV = new MonsterStats(ev0, ev1, ev2, ev3, ev4, ev5);

            var iv = dict["IVs"].Split(',');
            var iv0 = (short) (short.Parse(iv[0]) > 1 ? short.Parse(iv[0]) - 1 : short.Parse(iv[0]));
            var iv1 = (short) (short.Parse(iv[1]) > 1 ? short.Parse(iv[1]) - 1 : short.Parse(iv[1]));
            var iv2 = (short) (short.Parse(iv[2]) > 1 ? short.Parse(iv[2]) - 1 : short.Parse(iv[2]));
            var iv3 = (short) (short.Parse(iv[3]) > 1 ? short.Parse(iv[3]) - 1 : short.Parse(iv[3]));
            var iv4 = (short) (short.Parse(iv[4]) > 1 ? short.Parse(iv[4]) - 1 : short.Parse(iv[4]));
            var iv5 = (short) (short.Parse(iv[5]) > 1 ? short.Parse(iv[5]) - 1 : short.Parse(iv[5]));
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
