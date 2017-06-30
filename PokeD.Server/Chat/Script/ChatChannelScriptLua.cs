namespace PokeD.Server.Chat
{
    /*
    public class ChatChannelScriptLua : BaseChatChannelScript
    {
        private LuaScript Script { get; }
        private LuaTable Hook => Lua.ToLuaTable(Script["hook"]);

        public override string Name => (string) Script["Name"];
        public override string Description => (string) (Script["Description"] ?? string.Empty);
        public override string Aliases => (string) (Script["Alias"] ?? string.Empty);

        public ChatChannelScriptLua(LuaScript script) { Script = script; }

        public override bool Initialize() => Script.ReloadFile();

        public override bool MessageSend(ChatMessage chatMessage) => (bool) Hook.CallFunction("Call", "MessageSend", chatMessage)[0];

        public override bool Subscribe(Client client) => (bool) Hook.CallFunction("Call", "Subscribe", client)[0];

        public override bool UnSubscribe(Client client) => (bool) Hook.CallFunction("Call", "UnSubscribe", client)[0];
    }
    */
}