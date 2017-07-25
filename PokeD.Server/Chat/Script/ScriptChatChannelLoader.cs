using System.Collections.Generic;

namespace PokeD.Server.Chat
{
    public abstract class ScriptChatChannelLoader
    {
        public abstract IEnumerable<ScriptChatChannel> LoadChatChannels();
    }
}