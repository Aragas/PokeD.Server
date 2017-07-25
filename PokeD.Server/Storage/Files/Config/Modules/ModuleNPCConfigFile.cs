using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class ModuleNPCConfigFile : BaseFile, IConfigFile
    {
        public ConfigType ConfigType { get; }

        public ModuleNPCConfigFile(ConfigType configType) : base(new ModuleConfigFolder().CreateFile($"NPC{configType.GetFileExtension()}", CreationCollisionOption.OpenIfExists)) => ConfigType = configType;
    }
}