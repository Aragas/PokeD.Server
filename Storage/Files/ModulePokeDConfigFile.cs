using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class ModulePokeDConfigFile : BaseFile, IConfigFile
    {
        public ConfigType ConfigType { get; }

        public ModulePokeDConfigFile(ConfigType configType) : base(new ConfigFolder().CreateFile($"ModulePokeD{configType.GetFileExtension()}", CreationCollisionOption.OpenIfExists)) { ConfigType = configType; }
    }
}