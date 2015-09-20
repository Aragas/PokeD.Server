using System.IO;

using Newtonsoft.Json;

using PCLStorage;

using PokeD.Core.Wrappers;

using PokeD.Server.Clients;

namespace PokeD.Server.Extensions
{
    public static class FileSystemWrapperExtensions
    {
        public static bool LoadClientSettings(IClient p3DPlayer)
        {
            if (p3DPlayer.GameJoltId == 0)
                return false;

            var filename = $"{p3DPlayer.GameJoltId}.json";
            using (var stream = FileSystemWrapper.UsersFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                var file = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(file))
                    try { JsonConvert.PopulateObject(file, p3DPlayer); stream.Seek(0, SeekOrigin.Begin); writer.Write(JsonConvert.SerializeObject(p3DPlayer, Formatting.Indented)); }
                    catch (JsonReaderException) { return false; }
            }

            return true;
        }

        public static bool SaveClientSettings(IClient p3DPlayer)
        {
            if (p3DPlayer.GameJoltId == 0)
                return false;

            var filename = $"{p3DPlayer.GameJoltId}.json";
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
