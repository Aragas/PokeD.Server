using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class DatabaseComponentConfigFile : BaseFile, IConfigFile
    {
        public ConfigType ConfigType { get; }

        public DatabaseComponentConfigFile(ConfigType configType) : base(new ConfigFolder().CreateFile($"Database{configType.GetFileExtension()}", CreationCollisionOption.OpenIfExists)) { ConfigType = configType; }
    }
}