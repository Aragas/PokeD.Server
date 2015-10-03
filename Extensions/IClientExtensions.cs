using System.IO;

using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PCLStorage;

using PokeD.Server.Clients;

namespace PokeD.Server.Extensions
{
    public static class IClientExtensions
    {
        public static bool LoadClientSettings(this IClient p3DPlayer)
        {
            if (p3DPlayer.GameJoltID == 0)
                return false;

            var filename = $"{p3DPlayer.GameJoltID}.json";
            using (var stream = FileSystemWrapper.UsersFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                var file = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(file))
                {
                    try { JsonConvert.PopulateObject(file, p3DPlayer); stream.SetLength(0); writer.Write(JsonConvert.SerializeObject(p3DPlayer, Formatting.Indented)); }
                    catch (JsonReaderException) { stream.SetLength(0); writer.Write(JsonConvert.SerializeObject(p3DPlayer, Formatting.Indented)); return false; }
                    catch (JsonWriterException) { return false; }
                }
                else
                {
                    try { stream.SetLength(0); writer.Write(JsonConvert.SerializeObject(p3DPlayer, Formatting.Indented)); }
                    catch (JsonWriterException) { return false; }
                }

            }

            return true;
        }
        public static bool SaveClientSettings(this IClient p3DPlayer)
        {
            if (p3DPlayer.GameJoltID == 0)
                return false;

            var filename = $"{p3DPlayer.GameJoltID}.json";
            using (var stream = FileSystemWrapper.UsersFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
            {
                try { writer.Write(JsonConvert.SerializeObject(p3DPlayer, Formatting.Indented)); }
                catch (JsonWriterException) { return false; }
            }

            return true;
        }
    }
}
