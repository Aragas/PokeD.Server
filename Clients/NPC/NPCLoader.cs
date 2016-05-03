using System.Collections.Generic;
using System.Linq;

using PCLExt.FileStorage;
using PCLExt.Lua;

namespace PokeD.Server.Clients.NPC
{
    public static class NPCLoader
    {
        private const string Identifier = "npc_";
        private const string Extension = ".lua";

        public static List<Client> LoadNPCs(IServerModule server)
        {
            return new List<Client>(Storage.LuaFolder.GetFilesAsync().Result
                .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
                .Select(file => new NPCPlayer(GetNPCName(file.Name), Lua.CreateLuaScript(file.Name), server)));
        }

        private static string GetNPCName(string fileName) { return fileName.Remove(0, Identifier.Length).Remove(fileName.Length - Identifier.Length - Extension.Length, Extension.Length); }

    }
}
