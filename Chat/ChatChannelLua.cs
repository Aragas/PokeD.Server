using System;
using PCLExt.Lua;

using PokeD.Server.Clients;

namespace PokeD.Server.Chat
{
    public class ChatChannelLua : ChatChannel
    {
        private LuaScript Script { get; }
        private LuaTable Hook => Lua.ToLuaTable(Script["hook"]);

        public override string Name { get; protected set; }
        public override string Description { get; protected set; }
        public override string Alias { get; protected set; }

        public ChatChannelLua(LuaScript script)
        {
            Script = script;

            Script.ReloadFile();

            Name = (string) Script["Name"];
            Description = (string) Script["Description"];
            Alias = (string) Script["Alias"];
        }

        public override bool MessageSend(ChatMessage chatMessage) => (bool) Hook.CallFunction("Call", "MessageSend", chatMessage)[0];

        public override bool Subscribe(Client client) => (bool) Hook.CallFunction("Call", "Subscribe", client)[0];

        public override bool UnSubscribe(Client client) => (bool) Hook.CallFunction("Call", "UnSubscribe", client)[0];
    }
}