using PCLExt.FileStorage;

namespace PokeD.Server.Storage.Folders
{
    public class CrashLogsFolder : BaseFolder
    {
        public CrashLogsFolder() : base(new LogsFolder().CreateFolder("CrashLogs", CreationCollisionOption.OpenIfExists)) { }
    }
}