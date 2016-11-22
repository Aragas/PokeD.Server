using PCLExt.Config;
using PCLExt.Config.Extensions;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;

namespace PokeD.Server
{
    public class ModuleNPC : ServerModule
    {
        const string FileName = "ModuleNPC";

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        [ConfigIgnore]
        public override ushort Port { get; protected set; } = 0;

        #endregion Settings


        public ModuleNPC(Server server) : base(server) { }


        public override bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(Server.ConfigType, FileName, this);
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
        public override void Stop()
        {
            var status = FileSystemExtensions.SaveConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save NPC settings!");
            
            Logger.Log(LogType.Info, $"Stopping NPC.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped NPC.");
        }
        
        
        public override void StartListen() { }
        public override void CheckListener() { }


        private bool LoadNPCs()
        {
            Logger.Log(LogType.Info, "Loading NPC's.");
            var npcs = NPCLuaLoader.LoadNPCs(this);

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
            Server.DatabasePlayerGetID(client);
            Server.DatabasePlayerLoad(client);

            Clients.Add(client);

            Server.NotifyClientConnected(this, client);
        }
        public void RemoveClient(Client client, string reason = "")
        {
            Server.DatabasePlayerSave(client, true);

            Clients.Remove(client);

            Server.NotifyClientDisconnected(this, client);
        }


        public override void Update()
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();
        }


        public override void ClientConnected(Client client) { }
        public override void ClientDisconnected(Client client) { }

        public override void SendPrivateMessage(Client sender, Client destClient, string message, bool fromServer = false)
        {
            /*
            if (destClient is NPCPlayer)
                destClient.SendPacket(new ChatMessagePrivatePacket() { DestinationPlayerName = sender.Name, Message = message});
                //PokeDPlayerSendToClient(destClient, new ChatPrivateMessagePacket() { Message = message });
            else
                Server.NotifyClientPrivateMessage(this, sender, destClient, message);
            */
        }
        //public override void SendGlobalMessage(Client sender, string message, bool fromServer = false) { }
        //public override void SendServerMessage(Client sender, string message) { }

        public override void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false) { }
        public override void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false) { }
        public override void SendTradeCancel(Client sender, Client destClient, bool fromServer = false) { }

        public override void SendPosition(Client sender, bool fromServer = false)
        {
            if (sender is NPCPlayer)
            {
                Server.NotifyClientPosition(this, sender);
                //P3DPlayerSendToAllClients(sender.GetDataPacket(), sender.ID);
            }
            else
                ;//P3DPlayerSendToAllClients(sender.GetDataPacket(), sender.ID);
        }
        
        public void ExecuteCommand(string command) { }


        public override void Dispose()
        {
            for (int i = 0; i < Clients.Count; i++)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}