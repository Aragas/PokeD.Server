using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class ScriptsFolder : BaseFolder
    {
        public ScriptsFolder() : base(new ApplicationFolder().CreateFolder("Scripts", CreationCollisionOption.OpenIfExists)) { }
    }
}