namespace PokeD.Server.Commands
{
    /*
    public class CommandScriptLua : BaseCommandScript
    {
        private LuaScript Script { get; }
        private LuaTable Hook => Lua.ToLuaTable(Script["hook"]);

        public override string Name => (string) Script["Name"];
        public override string Description => (string)(Script["Description"] ?? string.Empty);
        public override IEnumerable<string> Aliases => Lua.ToLuaTable(Script["Aliases"]).ToList().Select(obj => obj.ToString().Replace("\"", ""));
        public override PermissionFlags Permission => ParsePermissionFlags((string)(Script["Permission"] ?? string.Empty));

        public override World World { set { Script["World"] = value; } }

        public CommandScriptLua(LuaScript script) { Script = script; }

        public override bool Initialize() => Script.ReloadFile();

        public override void Handle(Client client, string alias, string[] arguments) => Hook.CallFunction("Call", "Handle", client, alias, arguments);

        public override void Help(Client client, string alias) => Hook.CallFunction("Call", "Help", client, alias);
    }
    */
}