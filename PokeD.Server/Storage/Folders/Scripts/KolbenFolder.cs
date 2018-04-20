using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PCLExt.FileStorage;

using PokeD.Server.Storage.Files;

namespace PokeD.Server.Storage.Folders
{
    class KolbenFolder : BaseFolder
    {
        public KolbenFolder() : base(new ScriptsFolder().CreateFolder("Kolben", CreationCollisionOption.OpenIfExists)) { }

        public IList<KolbenFile> GetScriptFiles() => GetFiles("*.klb").Select(kolbenFile => new KolbenFile(kolbenFile)).ToList();
        public async Task<IList<KolbenFile>> GetScriptFilesAsync() => (await GetFilesAsync("*.klb")).Select(kolbenFile => new KolbenFile(kolbenFile)).ToList();
    }
}