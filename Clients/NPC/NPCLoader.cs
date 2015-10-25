using System.Collections.Generic;
using System.Linq;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Clients.NPC
{
    public static class NPCLoader
    {
        private const string Identifier = "npc_b_";
        private const string Extension = ".lua";

        public static List<IClient> LoadNPCs(Server server)
        {
            return new List<IClient>(FileSystemWrapper.LuaFolder.GetFilesAsync().Result
                .Where(file => file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
                .Select(file => new NPCPlayer(GetNPCName(file.Name), LuaWrapper.CreateLua(file.Name), server)));
        }

        private static string GetNPCName(string fileName) { return fileName.Remove(0, Identifier.Length).Remove(fileName.Length - Identifier.Length - Extension.Length, Extension.Length); }

    }
}
