using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class ServerConfigFile : BaseFile, IConfigFile
    {
        public ConfigType ConfigType { get; }

        public ServerConfigFile(ConfigType configType) : base(new ConfigFolder().CreateFile($"Server{configType.GetFileExtension()}", CreationCollisionOption.OpenIfExists)) { ConfigType = configType; }
    }
}
