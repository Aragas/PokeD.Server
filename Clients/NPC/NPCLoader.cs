using System.Collections.Generic;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Clients.NPC
{
    public static class NPCLoader
    {
        public static List<IClient> LoadNPCs(Server server)
        {
            var npcs = new List<IClient>();

            var files = FileSystemWrapper.LuaFolder.GetFilesAsync().Result;
            foreach (var file in files)
            {
                if (file.Name.ToLower().StartsWith("npc_") && file.Name.ToLower().EndsWith(".lua"))
                {
                    var name = file.Name.ToLower().Replace("npc_", "").Replace(".lua", "");
                    var lua = LuaWrapper.Create(file.Name);
                    var npc = new NPCPlayer(name, lua, server);
                    npcs.Add(npc);
                }
            }

            return npcs;
        }
    }
}
