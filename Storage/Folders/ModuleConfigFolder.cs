using PCLExt.FileStorage;

namespace PokeD.Server.Storage.Folders
{
    public class ModuleConfigFolder : BaseFolder
    {
        public ModuleConfigFolder() : base(new ConfigFolder().CreateFolder("Modules", CreationCollisionOption.OpenIfExists)) { }
    }
}