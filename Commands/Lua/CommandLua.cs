using System;
using System.Collections.Generic;
using System.Linq;

using PCLExt.Lua;

using PokeD.Server.Clients;

// ReSharper disable once CheckNamespace
namespace PokeD.Server.Commands
{
    public sealed class CommandLua : Command
    {
        private LuaScript Script { get; }
        private LuaTable Hook => Lua.ToLuaTable(Script["hook"]);

        public override string Name { get; protected set; }
        public override string Description { get; protected set; }

        public CommandLua(Server server, LuaScript script) : base(server)
        {
            Script = script;

            Script["World"] = World;

            Script.ReloadFile();

            Name = (string) Script["Name"];
            Description = (string) (Script["Description"] ?? string.Empty);
            Aliases = Lua.ToLuaTable(Script["Aliases"]).ToList().Select(obj => obj.ToString().Replace("\"", ""));
            Permissions = ParsePermissionFlags((string) (Script["Permission"] ?? string.Empty));
        }
        private static PermissionFlags ParsePermissionFlags(string permissionFlags)
        {
            var permissions = permissionFlags.Split(' ');
            var flags = new List<PermissionFlags>();
            foreach (var permission in permissions)
            {
                PermissionFlags flag;
                if (Enum.TryParse(permission, out flag))
                    flags.Add(flag);
            }

            var value = PermissionFlags.None;
            foreach (var flag in flags)
                value |= flag;
            return value;
        }

        public override void Handle(Client client, string alias, string[] arguments) => Hook.CallFunction("Call", "Handle", client, alias, arguments);

        public override void Help(Client client, string alias) => Hook.CallFunction("Call", "Help", client, alias);
    }
}