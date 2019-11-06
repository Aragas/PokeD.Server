using System;
using System.Collections.Generic;
using System.Linq;

using MoonSharp.Interpreter;

using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.Commands
{
    public class CommandScriptLua : BaseCommandScript
    {
        private LuaFile LuaFile { get; }
        private Script Script => LuaFile.Script;
        private Table Hook => (Table) Script.Globals["hook"];

        public override string Name => (string) Script.Globals["Name"];
        public override string Description => (string) Script.Globals["Description"] ?? string.Empty;
        public override IEnumerable<string> Aliases => ((Table) Script.Globals["Aliases"]).Values.Select(obj => obj.ToString().Replace("\"", ""));
        public override PermissionFlags Permission => ParsePermissionFlags((string) Script.Globals["Permission"] ?? string.Empty);

        public CommandScriptLua(IServiceContainer serviceContainer, LuaFile luaFile) : base(serviceContainer)
        {
            LuaFile = luaFile;
            Script.Globals["World"] = World;
        }

        private object[] CallHookFunction(params object[] args)
        {
            var ret = Script.Call(Hook["Call"], args).Tuple;
            return ret?.Any() == true ? ret.Select(dynVal => dynVal.ToObject()).ToArray() : Array.Empty<object>();
        }

        public override void Handle(Client client, string alias, string[] arguments) => CallHookFunction("Handle", client, alias, arguments);

        public override void Help(Client client, string alias) => CallHookFunction("Help", client, alias);
    }
}