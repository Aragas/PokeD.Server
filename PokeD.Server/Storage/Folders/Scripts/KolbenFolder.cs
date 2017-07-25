/*
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PCLExt.FileStorage;

using PokeD.Server.Storage.Files.Scripts;

namespace PokeD.Server.Storage.Folders.Scripts
{
    class KolbenFolder : BaseFolder
    {
        public KolbenFolder() : base(new ScriptsFolder().CreateFolder("Kolben", CreationCollisionOption.OpenIfExists)) { }

        public IList<KolbenFile> GetScriptFiles() => GetFiles("*.kol").Select(luaFile => new KolbenFile(luaFile)).ToList();
        public async Task<IList<KolbenFile>> GetScriptFilesAsync() => (await GetFilesAsync("*.kol")).Select(luaFile => new KolbenFile(luaFile)).ToList();
    }
}
*/