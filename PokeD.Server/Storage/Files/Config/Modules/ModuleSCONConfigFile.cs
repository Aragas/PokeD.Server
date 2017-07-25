using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class ModuleSCONConfigFile : BaseFile, IConfigFile
    {
        public ConfigType ConfigType { get; }

        public ModuleSCONConfigFile(ConfigType configType) : base(new ModuleConfigFolder().CreateFile($"SCON{configType.GetFileExtension()}", CreationCollisionOption.OpenIfExists)) => ConfigType = configType;
    }
}