using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class WorldComponentConfigFile : BaseFile, IConfigFile
    {
        public ConfigType ConfigType { get; }

        public WorldComponentConfigFile(ConfigType configType) : base(new ConfigFolder().CreateFile($"World{configType.GetFileExtension()}", CreationCollisionOption.OpenIfExists)) { ConfigType = configType; }
    }
}