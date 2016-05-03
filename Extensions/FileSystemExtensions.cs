using System.IO;

using PCLExt.Config;
using PCLExt.FileStorage;

namespace PokeD.Server.Extensions
{
    public static class FileSystemExtensions
    {
        public static bool LoadSettings<T>(ConfigType configType, string filename, T value)
        {
            var config = Config.Create(configType);

            using (var stream = Storage.SettingsFolder.CreateFileAsync($"{filename}.{config.FileExtension}", CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                var file = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(file))
                {
                    try
                    {
                        if (value == null)
                        {
                            value = config.Deserialize<T>(file);
                        }
                        else
                        {
                            config.PopulateObject(file, value);
                            stream.SetLength(0);
                            writer.Write(config.Serialize(value));
                        }
                    }
                    catch (ConfigDeserializingException)
                    {
                        stream.SetLength(0);
                        writer.Write(config.Serialize(value));
                        return false;
                    }
                    catch (ConfigSerializingException) { return false; }
                }
                else
                {
                    try
                    {
                        stream.SetLength(0);
                        writer.Write(config.Serialize(value));
                    }
                    catch (ConfigSerializingException) { return false; }
                }
            }

            return true;
        }
        public static bool SaveSettings<T>(ConfigType configType, string filename, T defaultValue = default(T))
        {
            var config = Config.Create(configType);

            using (var stream = Storage.SettingsFolder.CreateFileAsync($"{filename}.{config.FileExtension}", CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
            {
                try { writer.Write(config.Serialize(defaultValue)); }
                catch (ConfigSerializingException) { return false; }
            }

            return true;
        }

        public static bool LoadLog(string filename, out string content)
        {
            content = string.Empty;

            using (var stream = Storage.LogFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
            {
                try { writer.Write(content); }
                catch (IOException) { return false; }
            }

            return true;
        }
        public static bool SaveLog(string filename, string content)
        {
            using (var stream = Storage.LogFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).Result.OpenAsync(FileAccess.ReadAndWrite).Result)
            using (var writer = new StreamWriter(stream))
            {
                try { writer.Write(content); }
                catch (IOException) { return false; }
            }

            return true;
        }
    }
}
