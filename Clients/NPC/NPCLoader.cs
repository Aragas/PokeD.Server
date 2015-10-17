using System.Collections.Generic;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Clients.NPC
{
    public static class NPCLoader
    {
        private const string Identifier = "npc_b_";
        private const string Extension = ".lua";

        public static List<IClient> LoadNPCs(Server server)
        {
            var npcs = new List<IClient>();

            var files = FileSystemWrapper.LuaFolder.GetFilesAsync().Result;
            foreach (var file in files)
            {
                if (file.Name.ToLower().StartsWith(Identifier) && file.Name.ToLower().EndsWith(Extension))
                {
                    var name = GetNPCName(file.Name);
                    var lua = LuaWrapper.Create(file.Name);
                    var npc = new NPCPlayer(name, lua, server);
                    npcs.Add(npc);
                }
            }

            return npcs;
        }

        private static string GetNPCName(string fileName)
        {
            return fileName.Remove(0, Identifier.Length).Remove(fileName.Length - Identifier.Length - Extension.Length, Extension.Length);
        }

    }
}
