using System.IO;

using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PCLStorage;

using PokeD.Server.Clients;

namespace PokeD.Server.Extensions
{
    public static class IClientExtensions
    {
        public static bool LoadClientSettings(this IClient player)
        {
            var filename = player.GameJoltID == 0 ? $"{player.Name}.json" : $"{player.GameJoltID}.json";

            using (var stream = FileSystemWrapper.UsersFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                var file = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(file))
                {
                    try { JsonConvert.PopulateObject(file, player); stream.SetLength(0); writer.Write(JsonConvert.SerializeObject(player, Formatting.Indented)); }
                    catch (JsonReaderException) { stream.SetLength(0); writer.Write(JsonConvert.SerializeObject(player, Formatting.Indented)); return false; }
                    catch (JsonWriterException) { return false; }
                }
                else
                {
                    try { stream.SetLength(0); writer.Write(JsonConvert.SerializeObject(player, Formatting.Indented)); }
                    catch (JsonWriterException) { return false; }
                }

            }

            return true;
        }
        public static bool SaveClientSettings(this IClient player)
        {
            var filename = player.GameJoltID == 0 ? $"{player.Name}.json" : $"{player.GameJoltID}.json";

            using (var stream = FileSystemWrapper.UsersFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
            {
                try { writer.Write(JsonConvert.SerializeObject(player, Formatting.Indented)); }
                catch (JsonWriterException) { return false; }
            }

            return true;
        }
    }
}
