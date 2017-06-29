/*
using PCLExt.Config;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD;
using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;
using PokeD.Server.Storage.Files;

namespace PokeD.Server
{
    public class ModuleNPC : ServerModule<NPCPlayer, ModuleNPC>
    {
        protected override string ModuleName { get; } = "ModuleNPC";
        protected override IConfigFile ModuleConfigFile => new ModuleNPCConfigFile(Server.ConfigType);

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

            Logger.Log(LogType.Info, $"Starting {ModuleName}.");


            return true;
        }
        public override bool Stop()
        {
            if (!base.Stop())
                return false;


            Logger.Log(LogType.Info, $"Stopping {ModuleName}.");

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
            if (!Server.DatabaseSetClientID(client))
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

        public override void SendTradeRequest(Client sender, DataItems monster, Client destClient, bool fromServer = false) { }
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
*/