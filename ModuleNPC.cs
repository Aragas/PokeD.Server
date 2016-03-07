using Aragas.Core.Wrappers;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;

namespace PokeD.Server
{
    public class ModuleNPC : IServerModule
    {
        const string FileName = "ModuleNPC";

        #region Settings

        public bool Enabled { get; private set; } = false;

        [ConfigIgnore]
        public ushort Port => 0;

        #endregion Settings

        [ConfigIgnore]
        public Server Server { get; }

        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public bool ClientsVisible { get; } = true;


        public ModuleNPC(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load NPC settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"NPC not enabled!");
                return false;
            }

            LoadNPCs();

            Logger.Log(LogType.Info, $"Starting NPC.");

            return true;
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

        public void AddClient(Client client)
        {
            Server.PeekDBID(client);
            Server.LoadDBPlayer(client);

            Clients.Add(client);

            Server.ClientConnected(this, client);
        }
        public void RemoveClient(Client client, string reason = "")
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


        public void OtherConnected(Client client) { }
        public void OtherDisconnected(Client client) { }

        public void SendServerMessage(Client sender, string message) { }
        public void SendPrivateMessage(Client sender, Client destClient, string message)
        {
            if (destClient is NPCPlayer)
                destClient.SendPacket(new ChatMessagePrivatePacket() { DestinationPlayerName = sender.Name, Message = message});
                //PokeDPlayerSendToClient(destClient, new ChatPrivateMessagePacket() { Message = message });
            else
                Server.ClientPrivateMessage(this, sender, destClient, message);
        }
        public void SendGlobalMessage(Client sender, string message) { }

        public void SendTradeRequest(Client sender, Monster monster, Client destClient) { }
        public void SendTradeConfirm(Client sender, Client destClient) { }
        public void SendTradeCancel(Client sender, Client destClient) { }

        public void SendPosition(Client sender)
        {
            if (sender is NPCPlayer)
            {
                Server.ClientPosition(this, sender);
                //P3DPlayerSendToAllClients(sender.GetDataPacket(), sender.ID);
            }
            else
                ;//P3DPlayerSendToAllClients(sender.GetDataPacket(), sender.ID);
        }
        
        public void ExecuteCommand(string command) { }


        public void Dispose()
        {
            for (int i = 0; i < Clients.Count; i++)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}
