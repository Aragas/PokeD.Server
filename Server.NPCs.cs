using System.Collections.Generic;

using PokeD.Core.Packets.Server;

using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server
    {
        List<IClient> NPCs { get; set; } = new List<IClient>();


        private bool LoadNPCs()
        {
            Logger.Log(LogType.Info, "Loading NPC's.");
            var npcs = NPCLoader.LoadNPCs(this);

            foreach (var npc in npcs)
                AddNPC(npc);

            return true;
        }
        public bool ReloadNPCs()
        {
            foreach (var npc in NPCs)
                RemoveNPC(npc);

            return LoadNPCs();
        }

        private void AddNPC(IClient npc)
        {
            LoadDBNPC(npc);

            NPCs.Add(npc);

            SendToAllClients(new CreatePlayerPacket { PlayerID = npc.ID }, -1);
            SendToAllClients(npc.GetDataPacket(), npc.ID);

        }
        private void RemoveNPC(IClient npc)
        {
            UpdateDBNPC(npc);

            NPCs.Remove(npc);

            SendToAllClients(new DestroyPlayerPacket { PlayerID = npc.ID });
        }

        private void LoadDBNPC(IClient npc)
        {
            var data = Database.Find<Player>(p => p.Name == npc.Name);

            if (data != null)
            {
                var id = data.Id;
                var newData = new Player(npc, PlayerType.NPC) { Id = id };

                Database.Update(newData);

                npc.ID = data.Id;
            }
            else
            {
                Database.Insert(new Player(npc, PlayerType.NPC));
                npc.ID = Database.Find<Player>(p => p.Name == npc.Name).Id;
            }
        }
        private void UpdateDBNPC(IClient npc)
        {
            Database.Update(new Player(npc, PlayerType.NPC));
        }


        private void UpdateNPC()
        {
            for (var i = 0; i < NPCs.Count; i++)
                NPCs[i]?.Update();
        }
    }
}
