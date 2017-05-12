using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class ModuleP3DConfigFile : BaseFile, IConfigFile
    {
        public ConfigType ConfigType { get; }

        public ModuleP3DConfigFile(ConfigType configType) : base(new ConfigFolder().CreateFile($"ModuleP3D{configType.GetFileExtension()}", CreationCollisionOption.OpenIfExists)) { ConfigType = configType; }
    }
}