using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Aragas.Network.Packets;

using PCLExt.Config;

using PokeD.Core.Data.P3D;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Trade;
using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Clients.P3D;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Services;
using PokeD.Server.Storage.Files;

namespace PokeD.Server.Modules
{
    public class ModuleP3D : ServerModule
    {
        protected override string ComponentName { get; } = "ModuleP3D";
        protected override IConfigFile ComponentConfigFile => new ModuleP3DConfigFile(ConfigType);

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15124;

        public string ServerName { get; protected set; } = "Put Server Name Here";

        public string ServerMessage { get; protected set; } = "Put Server Description Here";
        
        public int MaxPlayers { get; protected set; } = 1000;

        public bool EnableOfflineAccounts { get; protected set; } = false;

        public bool ValidatePokemons { get; protected set; } = false;

        //public bool EncryptionEnabled { get; protected set; } = true;

        public bool MoveCorrectionEnabled { get; protected set; } = true;

        #endregion Settings

        private TcpListener Listener { get; set; }

        private CancellationTokenSource ListenerToken { get; set; }
        private CancellationTokenSource PlayerWatcherToken { get; set; }
        private CancellationTokenSource PlayerCorrectionToken { get; set; }

        private ConcurrentDictionary<string, P3DPlayer[]> NearPlayers { get; } = new ConcurrentDictionary<string, P3DPlayer[]>();

        private bool IsDisposing { get; set; }

        private ReaderWriterLockList<P3DPlayer> Clients { get; } = new ReaderWriterLockList<P3DPlayer>();


        public ModuleP3D(IServiceContainer services, ConfigType configType) : base(services, configType) { }

        public override bool Start()
        {
            if (!base.Start())
                return false;


            Logger.Log(LogType.Debug, $"Starting {ComponentName}.");

            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            Listener.Server.ReceiveTimeout = 5000;
            Listener.Server.SendTimeout = 5000;
            Listener.Start();

            ListenerToken = new CancellationTokenSource();
            new Thread(ListenerCycle)
            {
                Name = "ModuleP3DListenerThred",
                IsBackground = true
            }.Start();


            if (MoveCorrectionEnabled)
            {
                PlayerWatcherToken = new CancellationTokenSource();
                new Thread(PlayerWatcherCycle)
                {
                    Name = "ModuleP3DWatcherThread",
                    IsBackground = true
                }.Start();

                PlayerCorrectionToken = new CancellationTokenSource();
                new Thread(PlayerCorrectionCycle)
                {
                    Name = "ModuleP3DCorrectionThread",
                    IsBackground = true
                }.Start();
            }


            ModuleManager.ClientJoined += (this, ModuleManager_ClientJoined);
            ModuleManager.ClientLeaved += (this, ModuleManager_ClientLeaved);


            return true;
        }
        public override bool Stop()
        {
            if (!base.Stop())
                return false;


            Logger.Log(LogType.Debug, $"Stopping {ComponentName}.");

            ListenerToken?.Cancel();
            
            PlayerWatcherToken?.Cancel();
            PlayerCorrectionToken?.Cancel();

            ModuleManager.ClientJoined -= ModuleManager_ClientJoined;
            ModuleManager.ClientLeaved -= ModuleManager_ClientLeaved;

            Listener?.Stop();

            List<P3DPlayer> clientsToKick = Clients.ToList();
            for (var i = 0; i < clientsToKick.Count; i++)
                clientsToKick[i]?.SendKick("Server is closing!");

            Clients.Clear();
            NearPlayers.Clear();


            return true;
        }
       
        private void ModuleManager_ClientJoined(object sender, ClientJoinedEventArgs eventArgs)
        {
            SendPacketToAll(new CreatePlayerPacket { Origin = -1, PlayerID = eventArgs.Client.ID });
            var packet = eventArgs.Client.GetDataPacket();
            packet.Origin = eventArgs.Client.ID;
            SendPacketToAll(packet);
            SendPacketToAll(new ChatMessageGlobalPacket { Origin = -1, Message = $"Player {eventArgs.Client.Name} joined the game!" });
        }
        private void ModuleManager_ClientLeaved(object sender, ClientLeavedEventArgs eventArgs)
        {
            SendPacketToAll(new DestroyPlayerPacket { Origin = -1, PlayerID = eventArgs.Client.ID });
            SendPacketToAll(new ChatMessageGlobalPacket { Origin = -1, Message = $"Player {eventArgs.Client.Name} disconnected!" });
        }


        private void ListenerCycle()
        {
            while (!IsDisposing && !ListenerToken.IsCancellationRequested)
            {
                var client = new P3DPlayer(Listener.AcceptSocket(), this);
                client.Ready += OnClientReady;
                client.Disconnected += OnClientLeave;
                client.StartListening();
                Clients.Add(client);
            }
        }
        
        [ConfigIgnore]
        public static long PlayerWatcherThreadTime { get; private set; }
        private void PlayerWatcherCycle()
        {
            var watch = Stopwatch.StartNew();
            while (!IsDisposing && !PlayerWatcherToken.IsCancellationRequested)
            {
                var players = new List<P3DPlayer>(Clients);

                foreach (var player in players.Where(player => player.LevelFile != null && !NearPlayers.ContainsKey(player.LevelFile)))
                    NearPlayers.TryAdd(player.LevelFile, null);

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
                    PlayerWatcherThreadTime = watch.ElapsedMilliseconds;

                    var time = (int)(400 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }
        }

        [ConfigIgnore]
        public static long PlayerCorrectionThreadTime { get; private set; }
        private void PlayerCorrectionCycle()
        {
            var watch = Stopwatch.StartNew();
            while (!IsDisposing && !PlayerCorrectionToken.IsCancellationRequested)
            {
                foreach (var nearPlayers in NearPlayers.Where(nearPlayers => nearPlayers.Value != null))
                    foreach (var player in nearPlayers.Value.Where(player => player.Moving))
                        foreach (var playerToSend in nearPlayers.Value.Where(playerToSend => player != playerToSend))
                        {
                            var packet = player.GetDataPacket();
                            packet.Origin = player.ID;
                            playerToSend.SendPacket(packet);
                        }



                if (watch.ElapsedMilliseconds < 5)
                {
                    PlayerCorrectionThreadTime = watch.ElapsedMilliseconds;

                    var time = (int)(5 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }
        }


        public override IReadOnlyList<Client> GetClients() => Clients.ToList();

        protected override void OnClientReady(object sender, EventArgs eventArgs)
        {
            var client = sender as Client;
            // -- We assume the Client is a GameJolt or the Client's password is correct and no one is using the Client's name.

            (var isBanned, var banInfo) = ModuleManager.BanStatus(client);
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
            foreach (var aClient in ModuleManager.GetAllClients())
            {
                client.SendPacket(new CreatePlayerPacket { Origin = -1, PlayerID = aClient.ID });
                var packet = aClient.GetDataPacket();
                packet.Origin = aClient.ID;
                client.SendPacket(packet);
            }
            // Send to Players player ID
            SendPacketToAll(new CreatePlayerPacket { Origin = -1, PlayerID = client.ID });
            // Send to player his ID
            client.SendPacket(new CreatePlayerPacket { Origin = -1, PlayerID = client.ID });
            var p = client.GetDataPacket();
            p.Origin = client.ID;
            SendPacketToAll(p);

            base.OnClientReady(sender, eventArgs);
        }
        protected override void OnClientLeave(object sender, EventArgs eventArgs)
        {
            var client = sender as P3DPlayer;

            Clients.Remove(client);

            if (client.ID > 0)
                base.OnClientLeave(sender, eventArgs);

            client.Dispose();
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public override void Update()
        {
            //for (var i = Clients.Count - 1; i >= 0; i--)
            //    Clients[i]?.Update();


            if (UpdateWatch.ElapsedMilliseconds > 1000)
            {
                SendPacketToAll(new WorldDataPacket { Origin = -1, DataItems = World.GenerateDataItems() });

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
        }

        public void SendPacketToAll(Packet packet)
        {
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i]?.SendPacket(packet);
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

            var packet = sender.GetDataPacket();
            packet.Origin = sender.ID;
            SendPacketToAll(packet);
        }

        public override bool AssignID(Client client)
        {
            var p3dClient = client as P3DPlayer;
            if (p3dClient == null)
                return false;

            if (!EnableOfflineAccounts && !p3dClient.IsGameJoltPlayer)
            {
                client.SendKick("Offline accounts are disabled on this server! Log in using your GameJolt account!");
                return false;
            }

            if (ModuleManager.GetAllClients().Any(c => c != client && c.Nickname == client.Nickname) || IsGameJoltIDUsed(p3dClient))
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

            for (var i = Clients.Count - 1; i >= 0; i--)
            {
                var client = Clients[i];
                if (client != player && client.IsGameJoltPlayer && player.GameJoltID == client.GameJoltID)
                    return true;
            }

            //var t0 = Server.DatabaseGetAll<BanTable>().ToList();
            //var t1 = Server.DatabaseGetAll<ClientTable>().FirstOrDefault(ct => ct.);

            return false;
        }

        /// <summary>
        /// There is a possibility that someone was banned with existing id and chnaged his name. GJ allows it.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private (bool IsBanned, BanTable BanTable) BanStatusGJ(P3DPlayer client)
        {
            var table = Database.DatabaseGetAll<BanTable>().FirstOrDefault(banTable => Database.DatabaseGetAll<ClientGJTable>().Where(gjTable => gjTable.GameJoltID == client.GameJoltID).FirstOrDefault(table1 => banTable.ClientID == table1.ClientID) != null);
            return table != null ? (true, table) : (false, table);
        }


        public override void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;
        }
    }
}