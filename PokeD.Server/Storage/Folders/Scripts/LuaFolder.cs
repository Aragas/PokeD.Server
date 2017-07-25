using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PCLExt.FileStorage;

using PokeD.Server.Storage.Files;

namespace PokeD.Server.Storage.Folders
{
   
    public class LuaFolder : BaseFolder
    {
        public LuaFolder() : base(new ScriptsFolder().CreateFolder("Lua", CreationCollisionOption.OpenIfExists)) { }

        public IList<LuaFile> GetScriptFiles(LuaModules modules = LuaModules.Hook | LuaModules.Translator) => GetFiles("*.lua").Select(luaFile => new LuaFile(luaFile, modules)).ToList();
        public async Task<IList<LuaFile>> GetScriptFilesAsync(LuaModules modules = LuaModules.Hook | LuaModules.Translator) => (await GetFilesAsync("*.lua")).Select(luaFile => new LuaFile(luaFile, modules)).ToList();
    }
}