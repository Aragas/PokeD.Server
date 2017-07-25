using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PCLExt.FileStorage;
using PCLExt.FileStorage.Folders;

namespace PokeD.Server.Storage.Folders
{
    public class ModulesFolder : BaseFolder
    {
        public ModulesFolder() : base(new ApplicationFolder().CreateFolder("Modules", CreationCollisionOption.OpenIfExists)) { }

        public IList<ModuleFile> GetModuleFiles() => GetFiles("*.mod").Select(moduleFile => new ModuleFile(moduleFile)).ToList();
        public async Task<IList<ModuleFile>> GetModuleFilesAsync() => (await GetFilesAsync("*.mod")).Select(moduleFile => new ModuleFile(moduleFile)).ToList();
    }
}