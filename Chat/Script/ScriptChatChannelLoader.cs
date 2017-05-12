using System.Collections.Generic;

namespace PokeD.Server.Chat
{
    public static class ScriptChatChannelLoader
    {
        private const string Identifier = "chatchannel_";
        private const string Extension = ".lua";

        static ScriptChatChannelLoader()
        {
            /*
            Lua.RegisterModule(new LuaModulesFolder().HookFile);
            Lua.RegisterModule(new LuaModulesFolder().TranslatorFile);
            */
        }

        public static IEnumerable<ScriptChatChannel> LoadChatChannels()
        {
            return new List<ScriptChatChannel>();
            //return new LuaFolder().GetFiles()
            //.Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
            //.Select(file => new ScriptChatChannel(Lua.CreateLuaScript(file)));
        }
    }
}