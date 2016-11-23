using System.Collections.Generic;
using System.Linq;

using PCLExt.FileStorage;
using PCLExt.Lua;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public static class CommandLuaLoader
    {
        private const string Identifier = "command_";
        private const string Extension = ".lua";

        static CommandLuaLoader()
        {
            Lua.RegisterModule("hook");
            Lua.RegisterModule("translator");
        }

        public static IEnumerable<CommandLua> LoadCommands(Server server) => Storage.LuaFolder.GetFilesAsync().Result
            .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
            .Select(file => new CommandLua(server, Lua.CreateLuaScript(file.Name)));
    }
}