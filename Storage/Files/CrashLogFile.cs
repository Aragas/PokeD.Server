using System;

using PCLExt.FileStorage;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Storage.Files
{
    public class CrashLogFile : BaseFile
    {
        public CrashLogFile() : base(new CrashLogsFolder().CreateFile($"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log", CreationCollisionOption.OpenIfExists)) { }
    }
}