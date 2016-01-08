using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.PokeD.Monster;

using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;

namespace PokeD.Server
{
    public class ModuleNPC : IServerModule
    {
        const string FileName = "ModuleNPC.json";

        #region Settings


        
        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }

        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        [JsonIgnore]
        public bool ClientsVisible { get; } = true;


        public ModuleNPC(Server server) { Server = server; }


        public void Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load NPC settings!");

            LoadNPCs();

            Logger.Log(LogType.Info, $"Starting NPC.");

        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save NPC settings!");
            
            Logger.Log(LogType.Info, $"Stopping NPC.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped NPC.");
        }
        
        
        public void StartListen() { }
        public void CheckListener() { }


        private bool LoadNPCs()
        {
            Logger.Log(LogType.Info, "Loading NPC's.");
            var npcs = NPCLoader.LoadNPCs(this);

            foreach (var npc in npcs)
                AddClient(npc);

            return true;
        }
        public bool ReloadNPCs()
        {
            for (int i = 0; i < Clients.Count; i++)
                RemoveClient(Clients[i]);

            return LoadNPCs();
        }

        public void AddClient(IClient client)
        {
            Server.PeekDBID(client);
            Server.LoadDBPlayer(client);

            Clients.Add(client);

            Server.ClientConnected(this, client);
        }
        public void RemoveClient(IClient client, string reason = "")
        {
            Server.UpdateDBPlayer(client, true);

            Clients.Remove(client);

            Server.ClientDisconnected(this, client);
        }


        public void Update()
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();
        }


        public void OtherConnected(IClient client) { }
        public void OtherDisconnected(IClient client) { }

        public void SendServerMessage(string message) { }
        public void SendPrivateMessage(IClient sender, IClient destClient, string message) { }
        public void SendGlobalMessage(IClient sender, string message) { }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient) { }
        public void SendTradeConfirm(IClient sender, IClient destClient) { }
        public void SendTradeCancel(IClient sender, IClient destClient) { }

        public void ExecuteCommand(string command) { }


        public void Dispose()
        {
            for (int i = 0; i < Clients.Count; i++)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}
