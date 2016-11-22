using System.Collections.Generic;
using System.Linq;

using PCLExt.FileStorage;
using PCLExt.Lua;

namespace PokeD.Server.Chat
{
    public static class ChatChannelLuaLoader
    {
        private const string Identifier = "chatchannel_";
        private const string Extension = ".lua";

        static ChatChannelLuaLoader()
        {
            Lua.RegisterModule("hook");
            Lua.RegisterModule("translator");
        }

        public static IEnumerable<ChatChannelLua> LoadChatChannels() => Storage.LuaFolder.GetFilesAsync().Result
            .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
            .Select(file => new ChatChannelLua(Lua.CreateLuaScript(file.Name)));
    }
}