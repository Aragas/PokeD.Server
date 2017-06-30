/*
using System.Collections.Generic;

namespace PokeD.Server.Clients.NPC
{
    public static class NPCLuaLoader
    {
        static NPCLuaLoader()
        {
            //Lua.RegisterCustomFunc("Vector3", (Func<float, float, float, Vector3>) ((x, y, z) => new Vector3(x, y, z)));
            //Lua.RegisterCustomFunc("Vector2", (Func<float, float, Vector2>) ((x, y) => new Vector2(x, y)));
            //
            //Lua.RegisterModule(new LuaModulesFolder().HookFile);
            //Lua.RegisterModule(new LuaModulesFolder().TranslatorFile);
        }

        private const string Identifier = "npc_";
        private const string Extension = ".lua";

        public static List<Client> LoadAllNPC(ModuleNPC server)
        {
            return new List<Client>();
            //return new List<Client>(new LuaFolder().GetFiles()
            //    .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
            //    .Select(file => new NPCPlayer(Lua.CreateLuaScript(file), server)));
        }
    }
}
*/