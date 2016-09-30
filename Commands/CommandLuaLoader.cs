using System.Collections.Generic;
using System.Linq;

using PCLExt.FileStorage;
using PCLExt.Lua;

namespace PokeD.Server.Commands
{
    public static class CommandLuaLoader
    {
        private const string Identifier = "command_";
        private const string Extension = ".lua";

        public static IEnumerable<CommandLua> LoadCommands(CommandManager commandManager) => Storage.LuaFolder.GetFilesAsync().Result
            .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
            .Select(file => new CommandLua(commandManager, Lua.CreateLuaScript(file.Name)));
    }
}
