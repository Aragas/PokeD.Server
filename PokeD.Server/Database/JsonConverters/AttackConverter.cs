using System;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PokeD.BattleEngine.Attack;
using PokeD.Core.Data.PokeD;

namespace PokeD.Server.Database.JsonConverters
{
    public class AttackConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.GetTypeInfo().IsAssignableFrom(typeof(BaseAttackInstance).GetTypeInfo());

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var attack = value as BaseAttackInstance;
            var jo = new JObject
            {
                new JProperty("ID", attack.StaticData.ID),
                new JProperty("PPCurrent", attack.PPCurrent),
                new JProperty("PPUps", attack.PPUps),
            };
            jo.WriteTo(writer);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            short id = 1;
            if (jo.TryGetValue("ID", StringComparison.OrdinalIgnoreCase, out var idToken))
                id = idToken.ToObject<short>();

            byte ppCurrent = 0;
            if (jo.TryGetValue("PPCurrent", StringComparison.OrdinalIgnoreCase, out var ppCurrentToken))
                ppCurrent = ppCurrentToken.ToObject<byte>();

            byte ppUps = 0;
            if (jo.TryGetValue("PPUps", StringComparison.OrdinalIgnoreCase, out var ppUpsToken))
                ppUps = ppUpsToken.ToObject<byte>();

            return new Attack(id, ppCurrent, ppUps);
        }
    }
}