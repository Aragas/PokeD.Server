using System;
using System.Linq;

using PCLExt.Lua;

using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Commands
{
    public sealed class CommandLua : Command
    {
        static CommandLua()
        {
            LuaScript.RegisterType(typeof(World));
            LuaScript.RegisterType(typeof(Client));

            LuaScript.RegisterType(typeof(TimeSpan));
        }

        private LuaScript Script { get; }

        public override string Name { get; protected set; }
        public override string Description { get; protected set; }

        public CommandLua(CommandManager commandManager, LuaScript script) : base(commandManager)
        {
            Script = script;

            Script["World"] = World;

            Script.ReloadFile();

            Name = (string) Script["Name"];
            Description = (string) Script["Description"];
            Aliases = Lua.ToLuaTable(Script["Aliases"]).ToList().Select(obj => obj.ToString().Replace("\"", ""));
        }

        public override void Handle(Client client, string alias, string[] arguments) => Script.CallFunction("Handle", client, alias, arguments);

        public override void Help(Client client, string alias) => Script.CallFunction("Help", client, alias);
    }
}
