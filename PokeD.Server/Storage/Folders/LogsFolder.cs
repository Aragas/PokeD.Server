using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class LogsFolder : BaseFolder
    {
        public LogsFolder() : base(new ApplicationRootFolder().CreateFolder("Logs", CreationCollisionOption.OpenIfExists)) { }
    }
}