using System.Collections.Generic;
using System.Text;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeApi;
using PokeD.Core.Data.PokeD.Monster;

namespace PokeD.Server.Extensions
{
    public static class MonsterExtensions
    {
        public static DataItems ToDataItems(this Monster monster)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("Pokemon", $"[{monster.Species}]");
            dict.Add("Experience", $"[{monster.Experience}]");
            dict.Add("Gender", $"[{((int)monster.Gender)}]");
            dict.Add("EggSteps", $"[{monster.EggSteps}]");
            dict.Add("Item", $"[{monster.HeldItem}]");
            dict.Add("ItemData", $"[]");
            dict.Add("NickName", $"[{monster.DisplayName}]");
            dict.Add("Level", $"[{(monster.Level > 0 ? monster.Level - 1 : monster.Level)}]");
            dict.Add("OT", $"[{(monster.CatchInfo.TrainerID < 0 ? (ushort) -monster.CatchInfo.TrainerID : monster.CatchInfo.TrainerID)}]");
            dict.Add("Ability", $"[{string.Join(",", monster.Ability)}]");
            dict.Add("Status", $"[]"); // TODO
            dict.Add("Nature", $"[{monster.Nature}]");
            dict.Add("CatchLocation", $"[{monster.CatchInfo.Location}]");
            dict.Add("CatchTrainer", $"[{monster.CatchInfo.TrainerName}]");
            dict.Add("CatchBall", $"[{monster.CatchInfo.PokeballID}]");
            dict.Add("CatchMethod", $"[{monster.CatchInfo.Method}]");
            dict.Add("Friendship", $"[{monster.Friendship}]");
            dict.Add("isShiny", $"[{(monster.IsShiny ? 1 : 0)}]");

            var pp0 = PokeApiV2.GetMoves(new ResourceUri($"api/v2/move/{monster.Moves.Move_0.ID}/"))[0].pp;
            var pp1 = PokeApiV2.GetMoves(new ResourceUri($"api/v2/move/{monster.Moves.Move_1.ID}/"))[0].pp;
            var pp2 = PokeApiV2.GetMoves(new ResourceUri($"api/v2/move/{monster.Moves.Move_2.ID}/"))[0].pp;
            var pp3 = PokeApiV2.GetMoves(new ResourceUri($"api/v2/move/{monster.Moves.Move_3.ID}/"))[0].pp;
            dict.Add("Attack1", monster.Moves.Move_0.ID == 0 ? $"[]" : $"[{monster.Moves.Move_0.ID}, {pp0}, {pp0}]");
            dict.Add("Attack2", monster.Moves.Move_1.ID == 0 ? $"[]" : $"[{monster.Moves.Move_1.ID}, {pp1}, {pp1}]");
            dict.Add("Attack3", monster.Moves.Move_2.ID == 0 ? $"[]" : $"[{monster.Moves.Move_2.ID}, {pp2}, {pp2}]");
            dict.Add("Attack4", monster.Moves.Move_3.ID == 0 ? $"[]" : $"[{monster.Moves.Move_3.ID}, {pp3}, {pp3}]");

            dict.Add("HP", $"[{monster.CurrentHP}]");
            dict.Add("EVs", $"[{monster.InstanceData.EV.HP},{monster.InstanceData.EV.Attack},{monster.InstanceData.EV.Defense},{monster.InstanceData.EV.SpecialAttack},{monster.InstanceData.EV.SpecialDefense},{monster.InstanceData.EV.Speed}]");
            dict.Add("IVs", $"[{monster.InstanceData.IV.HP},{monster.InstanceData.IV.Attack},{monster.InstanceData.IV.Defense},{monster.InstanceData.IV.SpecialAttack},{monster.InstanceData.IV.SpecialDefense},{monster.InstanceData.IV.Speed}]");
            dict.Add("AdditionalData", $"[]");
            dict.Add("IDValue", $"[PokeD01Conv]");

            return DictionaryToDataItems(dict);
        }
        private static DataItems DictionaryToDataItems(Dictionary<string, string> dict)
        {
            var builder = new StringBuilder();

            foreach (var s in dict)
                builder.Append($"{{\"{s.Key}\"{s.Value}}}");

            return new DataItems(builder.ToString());
        }
    }
}