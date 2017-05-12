using PCLExt.FileStorage;

using PokeD.Core.Storage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class LogsFolder : BaseFolder
    {
        public LogsFolder() : base(new MainFolder().CreateFolder("Logs", CreationCollisionOption.OpenIfExists)) { }
    }
}