using PCLExt.FileStorage;

using PokeD.Core.Storage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class ConfigFolder : BaseFolder
    {
        public ConfigFolder() : base(new MainFolder().CreateFolder("Settings", CreationCollisionOption.OpenIfExists)) { }
    }
}
