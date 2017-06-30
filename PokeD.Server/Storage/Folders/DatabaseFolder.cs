using PCLExt.FileStorage;

using PokeD.Core.Storage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class DatabaseFolder : BaseFolder
    {
        public DatabaseFolder() : base(new MainFolder().CreateFolder("Database", CreationCollisionOption.OpenIfExists)) { }
    }
}