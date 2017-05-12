using System.Collections.Generic;

using Aragas.Network.Data;
using Aragas.Network.Packets;

using PCLExt.Config;
using PCLExt.Network;

using PokeD.Core.Data.PokeD;
using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;
using PokeD.Server.Storage.Files;

namespace PokeD.Server
{
    public class ModuleSCON : ServerModule
    {
        protected override string ModuleName { get; } = "ModuleSCON";
        protected override IConfigFile ModuleConfigFile => new ModuleSCONConfigFile(Server.ConfigType);

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15126;

        public PasswordStorage SCONPassword { get; protected set; } = new PasswordStorage();

        public bool EncryptionEnabled { get; protected set; } = true;
        
        #endregion Settings

        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        [ConfigIgnore]
        public override bool ClientsVisible { get; } = false;
        List<Client> PlayersJoining { get; } = new List<Client>();
        List<Client> PlayersToAdd { get; } = new List<Client>();
        List<Client> PlayersToRemove { get; } = new List<Client>();


        public ModuleSCON(Server server) : base(server) { }


        public override bool Start()
        {
            if (!base.Start())
                return false;


            Logger.Log(LogType.Info, $"Starting {ModuleName}.");

            Listener = SocketServer.CreateTCP(Port);
            Listener.Start();


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
        

        public override void AddClient(Client client)
        {
            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);

            base.AddClient(client);
        }
        public override void RemoveClient(Client client, string reason = "")
        {
            PlayersToRemove.Add(client);

            base.RemoveClient(client, reason);
        }


        public override void Update()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    PlayersJoining.Add(new SCONClient(Listener.AcceptTCPClient(), this));

            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Clients.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

                playerToRemove.Dispose();
            }

            #endregion Player Filtration

            #region Player Updating

            // Update actual players
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i]?.Update();

            // Update joining players
            for (var i = PlayersJoining.Count - 1; i >= 0; i--)
                PlayersJoining[i]?.Update();

            #endregion Player Updating
        }


        public override void ClientConnected(Client client) { }
        public override void ClientDisconnected(Client client) { }

        public void SendPacketToAll(Packet packet)
        {
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i]?.SendPacket(packet);
        }

        public override void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false) { }
        public override void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false) { }
        public override void SendTradeCancel(Client sender, Client destClient, bool fromServer = false) { }

        public override void SendPosition(Client sender, bool fromServer = false) { }


        public override void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            for (var i = PlayersJoining.Count - 1; i >= 0; i--)
                PlayersJoining[i].Dispose();
            PlayersJoining.Clear();

            for (var i = Clients.Count - 1; i >= 0; i--)
            {
                Clients[i].Kick("Closing server!");
                Clients[i].Dispose();
            }
            Clients.Clear();

            for (var i = PlayersToAdd.Count - 1; i >= 0; i--)
            {
                PlayersToAdd[i].Kick("Closing server!");
                PlayersToAdd[i].Dispose();
            }
            PlayersToAdd.Clear();

            // Do not dispose PlayersToRemove!
            PlayersToRemove.Clear();
        }
    }
}