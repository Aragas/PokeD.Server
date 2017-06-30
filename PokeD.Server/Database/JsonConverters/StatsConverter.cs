using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PokeD.BattleEngine.Monster.Data;

namespace PokeD.Server.Database.JsonConverters
{
    public class StatsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Stats);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var stats = value as Stats;
            var jo = new JObject
            {
                new JProperty("HP", stats.HP),
                new JProperty("ATK", stats.Attack),
                new JProperty("DEF", stats.Defense),
                new JProperty("SPATK", stats.SpecialAttack),
                new JProperty("SPDEF", stats.Defense),
                new JProperty("SPE", stats.Speed),
            };
            jo.WriteTo(writer);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            short hp = 1;
            if (jo.TryGetValue("HP", StringComparison.OrdinalIgnoreCase, out var hpToken))
                hp = hpToken.ToObject<short>();

            short atk = 1;
            if (jo.TryGetValue("ATK", StringComparison.OrdinalIgnoreCase, out var atkToken))
                atk = atkToken.ToObject<short>();

            short def = 1;
            if (jo.TryGetValue("DEF", StringComparison.OrdinalIgnoreCase, out var defToken))
                def = defToken.ToObject<short>();

            short spAtk = 1;
            if (jo.TryGetValue("SPATK", StringComparison.OrdinalIgnoreCase, out var spAtkToken))
                spAtk = spAtkToken.ToObject<short>();

            short spDef = 1;
            if (jo.TryGetValue("SPDEF", StringComparison.OrdinalIgnoreCase, out var spDefToken))
                spDefToken = spDefToken.ToObject<short>();

            short spe = 1;
            if (jo.TryGetValue("SPE", StringComparison.OrdinalIgnoreCase, out var speToken))
                spe = speToken.ToObject<short>();

            return new Stats(hp, atk, def, spAtk, spDef, spe);
        }
    }
}