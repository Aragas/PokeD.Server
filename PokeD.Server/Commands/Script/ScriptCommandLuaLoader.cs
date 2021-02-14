using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Commands
{
    public class ScriptCommandLuaLoader : ScriptCommandLoader
    {
        private const string Identifier = "command_";

        public override IEnumerable<ScriptCommand> LoadCommands(IServiceProvider serviceProvider) => new LuaFolder().GetScriptFiles()
            .Where(file => file.Name.ToLower().StartsWith(Identifier))
            .Select(file => new ScriptCommand(serviceProvider, new CommandScriptLua(serviceProvider, file)));
    }
}