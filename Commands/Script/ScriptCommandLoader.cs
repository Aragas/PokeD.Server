using System.Collections.Generic;

using PokeD.Core.Services;

namespace PokeD.Server.Commands
{
    public static class ScriptCommandLoader
    {
        private const string Identifier = "command_";
        private const string Extension = ".lua";

        static ScriptCommandLoader()
        {
            /*
            Lua.RegisterModule(new LuaModulesFolder().HookFile);
            Lua.RegisterModule(new LuaModulesFolder().TranslatorFile);
            */
        }

        public static IEnumerable<ScriptCommand> LoadCommands(IServiceContainer componentManager)
        {
            return new List<ScriptCommand>();
            //return new LuaFolder().GetFiles()
            //    .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
            //    .Select(file => new ScriptCommand(server, Lua.CreateLuaScript(file)));
        }
    }
}