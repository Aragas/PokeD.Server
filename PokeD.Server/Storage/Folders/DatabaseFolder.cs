using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class DatabaseFolder : BaseFolder
    {
        public DatabaseFolder() : base(new ApplicationRootFolder().CreateFolder("Database", CreationCollisionOption.OpenIfExists)) { }
    }
}