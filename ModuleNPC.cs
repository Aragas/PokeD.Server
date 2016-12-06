using Aragas.Network.Packets;

using PCLExt.Config;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;

namespace PokeD.Server
{
    public class ModuleNPC : ServerModule
    {
        protected override string ModuleFileName { get; } = "ModuleNPC";

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        [ConfigIgnore]
        public override ushort Port { get; protected set; } = 0;

        #endregion Settings


        public ModuleNPC(Server server) : base(server) { }


        public override bool Start()
        {
            if (!base.Stop())
                return false;


            LoadAllNPC();

            Logger.Log(LogType.Info, $"Starting {ModuleFileName}.");


            return true;
        }
        public override bool Stop()
        {
            if (!base.Stop())
                return false;


            Logger.Log(LogType.Info, $"Stopping {ModuleFileName}.");

            Dispose();


            return true;
        }
        

        private bool LoadAllNPC()
        {
            Logger.Log(LogType.Info, "Loading NPC's.");
            var npcs = NPCLuaLoader.LoadAllNPC(this);

            foreach (var npc in npcs)
                npc.Join();

            return true;
        }
        public bool ReloadAllNPC()
        {
            for (int i = Clients.Count - 1; i >= 0; i--)
                Clients[i].Kick();

            return LoadAllNPC();
        }


        public override void AddClient(Client client)
        {
            if (!Server.DatabaseSetClientId(client))
                return;

            //Server.DatabasePlayerLoad(client);
            ClientUpdate(client, true);

            Clients.Add(client);

            base.AddClient(client);
        }
        public override void RemoveClient(Client client, string reason = "")
        {
            ClientUpdate(client, true);

            Clients.Remove(client);

            base.RemoveClient(client, reason);
        }


        public override void Update()
        {
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i]?.Update();
        }


        public override void ClientConnected(Client client) { }
        public override void ClientDisconnected(Client client) { }

        public override void SendPacketToAll(Packet packet) { }

        public override void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false) { }
        public override void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false) { }
        public override void SendTradeCancel(Client sender, Client destClient, bool fromServer = false) { }

        public override void SendPosition(Client sender, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientPosition(this, sender);
        }
        

        public override void Dispose()
        {
            for (int i = Clients.Count - 1; i >= 0; i--)
                Clients[i].Dispose();
            Clients.Clear();
        }
    }
}