using System.IO;

using Newtonsoft.Json;

using PCLStorage;

using PokeD.Core.Wrappers;

using PokeD.Server.Data;

namespace PokeD.Server.Extensions
{
    public static class FileSystemWrapperExtensions
    {
        public static bool LoadUserSettings(ref Player player)
        {
            if (player.GameJoltId == 0)
                return false;

            var filename = string.Format("{0}.json", player.GameJoltId);
            using (var stream = FileSystemWrapper.UsersFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var reader = new StreamReader(stream))
            {
                var file = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(file))
                    try { JsonConvert.PopulateObject(file, player); }
                    catch (JsonReaderException) { return false; }
            }

            return true;
        }

        public static bool SaveUserSettings(Player player)
        {
            if (player.GameJoltId == 0)
                return false;

            var filename = string.Format("{0}.json", player.GameJoltId);
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
