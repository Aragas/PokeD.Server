using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Services;
using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Commands
{
    public class ScriptCommandLuaLoader : ScriptCommandLoader
    {
        private const string Identifier = "command_";

        public override IEnumerable<ScriptCommand> LoadCommands(IServiceContainer serviceContainer) => new LuaFolder().GetScriptFiles()
            .Where(file => file.Name.ToLower().StartsWith(Identifier))
            .Select(file => new ScriptCommand(serviceContainer, new CommandScriptLua(serviceContainer, file)));
    }
}