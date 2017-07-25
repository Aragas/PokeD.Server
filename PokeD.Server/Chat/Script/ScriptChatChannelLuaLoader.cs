using System.Collections.Generic;
using System.Linq;

using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Chat
{
    public class ScriptChatChannelLuaLoader : ScriptChatChannelLoader
    {
        private const string Identifier = "chatchannel_";

        public override IEnumerable<ScriptChatChannel> LoadChatChannels() => new LuaFolder().GetScriptFiles()
            .Where(file => file.Name.ToLower().StartsWith(Identifier))
            .Select(file => new ScriptChatChannel(new ChatChannelScriptLua(file)));
    }
}