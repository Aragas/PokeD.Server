using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using Aragas.Network.Data;

using PCLExt.Config;

using PokeD.Core.Data.P3D;
using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;
using PokeD.Server.Storage.Files;

namespace PokeD.Server
{
    public class ModuleSCON : ServerModule
    {
        protected override string ComponentName { get; } = "ModuleSCON";
        protected override IConfigFile ComponentConfigFile => new ModuleSCONConfigFile(ConfigType);

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15126;

        public PasswordStorage SCONPassword { get; protected set; } = new PasswordStorage();

        public bool EncryptionEnabled { get; protected set; } = true;
        
        #endregion Settings

        bool IsDisposing { get; set; }

        TcpListener Listener { get; set; }

        [ConfigIgnore]
        public override bool ClientsVisible { get; } = false;
        List<SCONClient> Clients { get; } = new List<SCONClient>();
        List<SCONClient> PlayersJoining { get; } = new List<SCONClient>();
        List<SCONClient> PlayersToAdd { get; } = new List<SCONClient>();
        List<SCONClient> PlayersToRemove { get; } = new List<SCONClient>();


        public ModuleSCON(IServiceContainer services, ConfigType configType) : base(services, configType) { }


        public override bool Start()
        {
            if (!base.Start())
                return false;


            Logger.Log(LogType.Debug, $"Starting {ComponentName}.");

            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            Listener.Start();


            return true;
        }
        public override bool Stop()
        {
            if (!base.Stop())
                return false;


            Logger.Log(LogType.Debug, $"Stopping {ComponentName}.");

            Dispose();


            return true;
        }


        public override IReadOnlyList<Client> GetClients() => Clients;

        protected override void OnClientReady(object sender, EventArgs eventArgs)
        {
            var client = sender as SCONClient;

            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);

            base.OnClientReady(sender, eventArgs);
        }
        protected override void OnClientLeave(object sender, EventArgs eventArgs)
        {
            var client = sender as SCONClient;

            PlayersToRemove.Add(client);

            base.OnClientLeave(sender, eventArgs);
        }


        public override void Update()
        {
            if (Listener?.Pending() == true)
                PlayersJoining.Add(new SCONClient(Listener.AcceptSocket(), this));

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


        public override void OnTradeRequest(Client sender, DataItems monster, Client destClient) { }
        public override void OnTradeConfirm(Client sender, Client destClient) { }
        public override void OnTradeCancel(Client sender, Client destClient) { }

        public override void OnPosition(Client sender) { }


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
                Clients[i].SendKick("Closing server!");
                Clients[i].Dispose();
            }
            Clients.Clear();

            for (var i = PlayersToAdd.Count - 1; i >= 0; i--)
            {
                PlayersToAdd[i].SendKick("Closing server!");
                PlayersToAdd[i].Dispose();
            }
            PlayersToAdd.Clear();

            // Do not dispose PlayersToRemove!
            PlayersToRemove.Clear();
        }
    }
}