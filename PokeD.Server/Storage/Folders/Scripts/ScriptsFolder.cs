using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class ScriptsFolder : BaseFolder
    {
        public ScriptsFolder() : base(new ApplicationRootFolder().CreateFolder("Scripts", CreationCollisionOption.OpenIfExists)) { }
    }
}