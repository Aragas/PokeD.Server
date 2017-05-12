using System;

using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class LogFile : BaseFile
    {
        public LogFile() : base(new LogsFolder().CreateFile($"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log", CreationCollisionOption.OpenIfExists)) { }
    }
}
