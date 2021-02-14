using Aragas.Network.Packets;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PokeD.Core.Data.P3D;
using PokeD.Core.Packets.P3D;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Trade;
using PokeD.Server.Clients;
using PokeD.Server.Clients.P3D;
using PokeD.Server.Database;
using PokeD.Server.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace PokeD.Server.Modules
{
    public class ModuleP3DOptions
    {
        public bool Enabled { get; protected set; } = true;
        public ushort Port { get; protected set; } = 15124;

        public string ServerName { get; protected set; } = "Put Server Name Here";
        public string ServerMessage { get; protected set; } = "Put Server Description Here";
        public int MaxPlayers { get; protected set; } = 1000;

        public bool EnableOfflineAccounts { get; protected set; } = true;
        public bool ValidatePokemons { get; protected set; } = false;
        public bool MoveCorrectionEnabled { get; protected set; } = true;
    }

    public class ModuleP3D : ServerModule
    {
        #region Settings

        public override bool Enabled { get => Options.Enabled; protected set { } }
        public override ushort Port { get => Options.Port; protected set { } }

        #endregion Settings

        private TcpListener Listener { get; set; }

        private ConcurrentDictionary<string, P3DPlayer[]?> NearPlayers { get; } = new();

        private List<P3DPlayer> JoiningClients { get; } = new();
        private List<P3DPlayer> Clients { get; } = new();

        private Task? _updateTask;
        private Task? _watcherTask;
        private Task? _correctorTask;
        private readonly CancellationTokenSource _stoppingCts = new();

        private readonly ILogger _logger;
        public readonly ModuleP3DOptions Options;

        public ModuleP3D(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<ModuleP3D>>();
            Options = serviceProvider.GetRequiredService<IOptions<ModuleP3DOptions>>().Value;
        }

        public override async Task StartAsync(CancellationToken ct)
        {
            _logger.LogDebug($"Starting {nameof(ModuleP3D)}.");

            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            Listener.Server.ReceiveTimeout = 5000;
            Listener.Server.SendTimeout = 5000;
            Listener.Start();

            _updateTask = ListenerCycleAsync(_stoppingCts.Token);

            if (Options.MoveCorrectionEnabled)
            {
                _watcherTask = PlayerWatcherCycleAsync(_stoppingCts.Token);
                _correctorTask = PlayerCorrectionCycleAsync(_stoppingCts.Token);
            }


            ModuleManager.ClientJoined += (this, ModuleManager_ClientJoined);
            ModuleManager.ClientLeaved += (this, ModuleManager_ClientLeaved);
        }
        public override async Task StopAsync(CancellationToken ct)
        {
            _logger.LogDebug($"Stopping {nameof(ModuleP3D)}.");
            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAll(
                    Task.WhenAny(_updateTask, Task.Delay(Timeout.Infinite, ct)),
                    Task.WhenAny(_watcherTask, Task.Delay(Timeout.Infinite, ct)),
                    Task.WhenAny(_correctorTask, Task.Delay(Timeout.Infinite, ct))
                );
            }

            ModuleManager.ClientJoined -= ModuleManager_ClientJoined;
            ModuleManager.ClientLeaved -= ModuleManager_ClientLeaved;

            Listener?.Stop();

            lock (JoiningClients)
            {
                foreach (var client in JoiningClients)
                    client?.Dispose();
                JoiningClients.Clear();
            }

            lock (Clients)
            {
                foreach (var client in Clients)
                    client?.SendKick("Server is closing!");
                Clients.Clear();
            }

            NearPlayers.Clear();
        }

        private void ModuleManager_ClientJoined(object sender, ClientJoinedEventArgs eventArgs)
        {
            var player = eventArgs.Client as P3DPlayer;

            lock (JoiningClients)
                JoiningClients.Remove(player);

            lock (Clients)
                Clients.Add(player);


            SendPacketToAll(new CreatePlayerPacket { Origin = Origin.Server, PlayerID = eventArgs.Client.ID });
            SendPacketToAll(eventArgs.Client.GetDataPacket());
            SendPacketToAll(new ChatMessageGlobalPacket { Origin = Origin.Server, Message = $"Player {eventArgs.Client.Name} joined the game!" });
        }
        private void ModuleManager_ClientLeaved(object sender, ClientLeavedEventArgs eventArgs)
        {
            SendPacketToAll(new DestroyPlayerPacket { Origin = Origin.Server, PlayerID = eventArgs.Client.ID });
            SendPacketToAll(new ChatMessageGlobalPacket { Origin = Origin.Server, Message = $"Player {eventArgs.Client.Name} disconnected!" });
        }


        private async Task ListenerCycleAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested) // Listener.Stop() will stop it.
                {

                    var client = new P3DPlayer(await Listener.AcceptSocketAsync(), this);
                    client.Ready += OnClientReady;
                    client.Disconnected += OnClientLeave;
                    client.StartListening();

                    lock (JoiningClients)
                        JoiningClients.Add(client);
                }

            }
            catch (Exception e) when (e is SocketException) { }
        }

        private async Task PlayerWatcherCycleAsync(CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            while (!ct.IsCancellationRequested)
            {
                List<P3DPlayer> players;
                lock (Clients)
                    players = new List<P3DPlayer>(Clients);

                foreach (var player in players.Where(player => player.LevelFile is not null && !NearPlayers.ContainsKey(player.LevelFile)))
                    NearPlayers.TryAdd(player.LevelFile!, null);

                foreach (var level in NearPlayers.Keys)
                {
                    var playerList = new List<P3DPlayer>();
                    foreach (var player in players.Where(player => level == player.LevelFile))
                        playerList.Add(player);

                    var array = playerList.ToArray();
                    NearPlayers.AddOrUpdate(level, array, (s, players1) => players1 = array);
                }



                if (watch.ElapsedMilliseconds < 400)
                {
                    var time = (int) (400 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    await Task.Delay(time, ct);
                }
                watch.Reset();
                watch.Start();
            }
        }

        private async Task PlayerCorrectionCycleAsync(CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();
            while (!ct.IsCancellationRequested)
            {
                foreach (var nearPlayers in NearPlayers.Where(nearPlayers => nearPlayers.Value != null))
                    foreach (var player in nearPlayers.Value.Where(player => player.Moving))
                        foreach (var playerToSend in nearPlayers.Value.Where(playerToSend => player != playerToSend))
                        {
                            playerToSend.SendPacket(player.GetDataPacket());
                        }



                if (watch.ElapsedMilliseconds < 10)
                {
                    var time = (int) (5 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    await Task.Delay(time, ct);
                }
                watch.Reset();
                watch.Start();
            }
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
            if (sender is P3DPlayer client)
            {
                // -- We assume the Client is a GameJolt or the Client's password is correct and no one is using the Client's name.

                var (isBanned, banInfo) = ModuleManager.BanStatus(client);
                if (isBanned)
                {
                    client.SendBan(banInfo);
                    return;
                }

                (isBanned, banInfo) = BanStatusGJ(client as P3DPlayer);
                if (isBanned)
                {
                    client.SendBan(banInfo);
                    return;
                }

                // Send to player all Players ID
                lock (Clients)
                    foreach (var aClient in Clients)
                    {
                        client.SendPacket(new CreatePlayerPacket {Origin = Origin.Server, PlayerID = aClient.ID});
                        client.SendPacket(aClient.GetDataPacket());
                    }

                // Send to Players player ID
                SendPacketToAll(new CreatePlayerPacket {Origin = Origin.Server, PlayerID = client.ID});
                // Send to player his ID
                client.SendPacket(new CreatePlayerPacket {Origin = Origin.Server, PlayerID = client.ID});
                SendPacketToAll(client.GetDataPacket());

                base.OnClientReady(client, eventArgs);
            }
        }
        protected override void OnClientLeave(Client sender, EventArgs eventArgs)
        {
            if (sender is P3DPlayer client)
            {
                lock (Clients)
                    Clients.Remove(client);

                if (client.ID > 0)
                    base.OnClientLeave(sender, eventArgs);

                client.Dispose();
            }
        }


        private Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public override async Task UpdateAsync(CancellationToken ct)
        {
            if (UpdateWatch.ElapsedMilliseconds > 1000)
            {
                SendPacketToAll(new WorldDataPacket { Origin = Origin.Server, DataItems = World.GenerateDataItems() });

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
        }

        public void SendPacketToAll<TPacket>(TPacket packet) where TPacket : Packet, new()
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                    client?.SendPacket(packet);
            }
        }

        public override void OnTradeRequest(Client sender, DataItems monster, Client destClient)
        {
            base.OnTradeRequest(sender, monster, destClient);

            if (destClient is P3DPlayer)
                destClient.SendPacket(new TradeOfferPacket { Origin = sender.ID, DataItems = monster });
        }
        public override void OnTradeConfirm(Client sender, Client destClient)
        {
            base.OnTradeConfirm(sender, destClient);

            if (destClient is P3DPlayer)
                destClient.SendPacket(new TradeStartPacket { Origin = sender.ID });
        }
        public override void OnTradeCancel(Client sender, Client destClient)
        {
            base.OnTradeCancel(sender, destClient);

            if (destClient is P3DPlayer)
                destClient.SendPacket(new TradeQuitPacket { Origin = sender.ID });
        }

        public override void OnPosition(Client sender)
        {
            base.OnPosition(sender);

            SendPacketToAll(sender.GetDataPacket());
        }

        public override bool AssignID(Client client)
        {
            if (!(client is P3DPlayer p3dClient))
                return false;

            if (!Options.EnableOfflineAccounts && !p3dClient.IsGameJoltPlayer)
            {
                client.SendKick("Offline accounts are disabled on this server! Log in using your GameJolt account!");
                return false;
            }

            if (ModuleManager.AllClientsSelect(clients => clients.Any(c => c != client && c.Nickname == client.Nickname)) || IsGameJoltIDUsed(p3dClient))
            {
                client.SendKick("You are already on server!");
                return false;
            }

            if (p3dClient.IsGameJoltPlayer)
            {
                var clientGJTable = Database.DatabaseGetAll<ClientGJTable>().FirstOrDefault(table => table.GameJoltID == p3dClient.GameJoltID);
                if (clientGJTable == null)
                {
                    var clientTable = new ClientTable(client);
                    Database.DatabaseSet(clientTable);
                    client.Load(clientTable);
                    clientGJTable = new ClientGJTable(client.ID, p3dClient.GameJoltID);
                    Database.DatabaseSet(clientGJTable);
                    return true;
                }
                else
                {
                    var clientTable = Database.DatabaseGet<ClientTable>(clientGJTable.ClientID);
                    client.Load(clientTable);
                    return true;
                }
            }
            else
                return base.AssignID(client);
        }

        private bool IsGameJoltIDUsed(P3DPlayer player)
        {
            if (!player.IsGameJoltPlayer)
                return false;

            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    if (client != null && client != player && client.IsGameJoltPlayer && player.GameJoltID == client.GameJoltID)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// There is a possibility that someone was banned with existing id and chnaged his name. GJ allows it.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private (bool IsBanned, BanTable BanTable) BanStatusGJ(P3DPlayer client)
        {
            var table = Database.DatabaseGetAll<BanTable>().FirstOrDefault(banTable => Database.DatabaseGetAll<ClientGJTable>().Where(gjTable => gjTable.GameJoltID == client.GameJoltID).Any(table1 => banTable.ClientID == table1.ClientID));
            return table != null ? (true, table) : (false, null);
        }
    }
}