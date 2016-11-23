using System;
using System.Collections.Generic;
using System.Linq;

using Aragas.Network.Data;

using PCLExt.FileStorage;
using PCLExt.Lua;

namespace PokeD.Server.Clients.NPC
{
    public static class NPCLuaLoader
    {
        static NPCLuaLoader()
        {
            Lua.RegisterCustomFunc("Vector3", (Func<float, float, float, Vector3>) ((x, y, z) => new Vector3(x, y, z)));
            Lua.RegisterCustomFunc("Vector2", (Func<float, float, Vector2>) ((x, y) => new Vector2(x, y)));

            Lua.RegisterModule("hook");
            Lua.RegisterModule("translator");
        }

        private const string Identifier = "npc_";
        private const string Extension = ".lua";

        public static List<Client> LoadNPCs(ModuleNPC server)=>
            new List<Client>(Storage.LuaFolder.GetFilesAsync().Result
                .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
                .Select(file => new NPCPlayer(Lua.CreateLuaScript(file.Name), server)));
    }
}