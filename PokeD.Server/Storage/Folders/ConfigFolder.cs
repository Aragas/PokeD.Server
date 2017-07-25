using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class ConfigFolder : BaseFolder
    {
        public ConfigFolder() : base(new ApplicationFolder().CreateFolder("Settings", CreationCollisionOption.OpenIfExists)) { }
    }
}