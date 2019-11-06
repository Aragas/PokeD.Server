using System;
using System.Linq;

using MoonSharp.Interpreter;

using PokeD.Server.Clients;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.Chat
{
    public class ChatChannelScriptLua : BaseChatChannelScript
    {
        private LuaFile LuaFile { get; }
        private Script Script => LuaFile.Script;
        private Table Hook => (Table) Script.Globals["hook"];

        public override string Name => (string)Script.Globals["Name"];
        public override string Description => (string) Script.Globals["Description"] ?? string.Empty;
        public override string Aliases => (string) Script.Globals["Alias"] ?? string.Empty;

        public ChatChannelScriptLua(LuaFile luaFile) { LuaFile = luaFile; }

        private object[] CallHookFunction(params object[] args)
        {
            var ret = Script.Call(Hook["Call"], args).Tuple;
            return ret?.Any() == true ? ret.Select(dynVal => dynVal.ToObject()).ToArray() : Array.Empty<object>();
        }

        public override bool MessageSend(ChatMessage chatMessage) => (bool) CallHookFunction("MessageSend", chatMessage)[0];

        public override bool Subscribe(Client client) => (bool) CallHookFunction("Subscribe", client)[0];

        public override bool UnSubscribe(Client client) => (bool) CallHookFunction("UnSubscribe", client)[0];
    }
}