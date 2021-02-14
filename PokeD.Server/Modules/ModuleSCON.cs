using Aragas.Network.Data;
using PCLExt.Config;
using PokeD.Core.Data.P3D;
using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;
using PokeD.Server.Services;
using PokeD.Server.Storage.Files;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PokeD.Server.Modules
{
    public class ModuleSCON : ServerModule
    {
        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15126;

        public PasswordStorage SCONPassword { get; protected set; } = new();

        public bool EncryptionEnabled { get; protected set; } = true;


        private TcpListener Listener { get; set; }

        [ConfigIgnore]
        public override bool ClientsVisible { get; } = false;
        private List<SCONClient> Clients { get; } = new();
        private List<SCONClient> PlayersJoining { get; } = new();
        private List<SCONClient> PlayersToAdd { get; } = new();
        private List<SCONClient> PlayersToRemove { get; } = new();

        private readonly ILogger _logger;

        public ModuleSCON(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }


        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Starting {nameof(ModuleSCON)}.");

            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            Listener.Start();
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace($"Stopping {nameof(ModuleSCON)}.");
        }

        public override void ClientsForeach(Action<IReadOnlyList<Client>> action)
        {
            lock (Clients)
                action(Clients);
        }
        public override TResult ClientsSelect<TResult>(Func<IReadOnlyList<Client>, TResult> func)
        {
            lock (Clients)
                return func(Clients);
        }
        public override IReadOnlyList<TResult> ClientsSelect<TResult>(Func<IReadOnlyList<Client>, IReadOnlyList<TResult>> func)
        {
            lock (Clients)
                return func(Clients);
        }

        protected override void OnClientReady(Client sender, EventArgs eventArgs)
        {
            var client = sender as SCONClient;

            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);

            base.OnClientReady(sender, eventArgs);
        }
        protected override void OnClientLeave(Client sender, EventArgs eventArgs)
        {
            var client = sender as SCONClient;

            PlayersToRemove.Add(client);

            base.OnClientLeave(sender, eventArgs);
        }


        public override async Task UpdateAsync(CancellationToken ct)
        {
            if (Listener?.Pending() == true)
                PlayersJoining.Add(new SCONClient(await Listener.AcceptSocketAsync(), this));

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
                await Clients[i].UpdateAsync(ct);

            // Update joining players
            for (var i = PlayersJoining.Count - 1; i >= 0; i--)
                await PlayersJoining[i].UpdateAsync(ct);

            #endregion Player Updating
        }


        public override void OnTradeRequest(Client sender, DataItems monster, Client destClient) { }
        public override void OnTradeConfirm(Client sender, Client destClient) { }
        public override void OnTradeCancel(Client sender, Client destClient) { }

        public override void OnPosition(Client sender) { }


        public void Dispose()
        {
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