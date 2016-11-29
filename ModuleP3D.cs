using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Aragas.Network.Packets;

using PCLExt.Config.Extensions;
using PCLExt.Network;
using PCLExt.Thread;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Extensions;
using PokeD.Core.Packets.P3D;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Trade;
using PokeD.Server.Clients;
using PokeD.Server.Clients.P3D;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public class ModuleP3D : ServerModule
    {
        const string FileName = "ModuleP3D";

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15124;

        public string ServerName { get; protected set; } = "Put name here";

        public string ServerMessage { get; protected set; } = "Put description here";
        
        public int MaxPlayers { get; protected set; } = 1000;

        public bool ValidatePokemons { get; protected set; } = false;

        public bool EncryptionEnabled { get; protected set; } = true;

        public bool MoveCorrectionEnabled { get; protected set; } = true;

        #endregion Settings

        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        IThread PlayerWatcherThread { get; set; }
        IThread PlayerCorrectionThread { get; set; }


        List<Client> PlayersJoining { get; } = new List<Client>();
        List<Client> PlayersToAdd { get; } = new List<Client>();
        List<Client> PlayersToRemove { get; } = new List<Client>();


        ConcurrentDictionary<string, P3DPlayer[]> NearPlayers { get; } = new ConcurrentDictionary<string, P3DPlayer[]>();

        public ModuleP3D(Server server) : base(server) { }


        public override bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load P3D settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"P3D not enabled!");
                return false;
            }

            Logger.Log(LogType.Info, $"Starting P3D.");
            
            if (MoveCorrectionEnabled)
            {
                PlayerWatcherThread = Thread.Create(PlayerWatcherCycle);
                PlayerWatcherThread.Name = "PlayerWatcherThread";
                PlayerWatcherThread.IsBackground = true;
                PlayerWatcherThread.Start();

                PlayerCorrectionThread = Thread.Create(PlayerCorrectionCycle);
                PlayerCorrectionThread.Name = "PlayerCorrectionThread";
                PlayerCorrectionThread.IsBackground = true;
                PlayerCorrectionThread.Start();
            }

            return true;
        }
        public override void Stop()
        {
            var status = FileSystemExtensions.SaveConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save P3D settings!");

            Logger.Log(LogType.Info, $"Stopping P3D.");

            if (PlayerWatcherThread.IsRunning)
                PlayerWatcherThread.Abort();

            if (PlayerCorrectionThread.IsRunning)
                PlayerCorrectionThread.Abort();

            Dispose();

            Logger.Log(LogType.Info, $"Stopped P3D.");
        }


        public static long PlayerWatcherThreadTime { get; private set; }
        private void PlayerWatcherCycle()
        {
            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
            {
                var players = new List<P3DPlayer>(Clients.GetTypeEnumerator<P3DPlayer>());
                //var players = new List<Client>(Clients.GetConcreteTypeEnumerator<P3DPlayer, ProtobufPlayer>());

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

        public static long PlayerCorrectionThreadTime { get; private set; }
        private void PlayerCorrectionCycle()
        {
            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
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


        public override void StartListen()
        {
            Listener = SocketServer.CreateTCP(Port);
            Listener.Start();
        }
        public override void CheckListener()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    PlayersJoining.Add(new P3DPlayer(Listener.AcceptTCPClient(), this));
        }


        public override void AddClient(Client client)
        {
            if (IsGameJoltIDUsed(client as P3DPlayer))
            {
                RemoveClient(client, "You are already on server!");
                return;
            }
            SavePlayerGJ(client as P3DPlayer);

            if (!Server.DatabasePlayerLoad(client))
            {
                RemoveClient(client, "Wrong password or you are already on server!");
                return;
            }


            // Send to player his ID
            client.SendPacket(new CreatePlayerPacket { Origin = -1, PlayerID = client.ID });
            // Send to player all Players ID
            foreach (var aClient in Server.GetAllClients())
            {
                client.SendPacket(new CreatePlayerPacket { Origin = -1, PlayerID = aClient.ID });
                var packet = aClient.GetDataPacket();
                packet.Origin = aClient.ID;
                client.SendPacket(packet);
            }
            // Send to Players player ID
            SendPacketToAll(new CreatePlayerPacket { Origin = -1, PlayerID = client.ID });
            var p = client.GetDataPacket();
            p.Origin = client.ID;
            SendPacketToAll(p);


            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);
        }
        public override void RemoveClient(Client client, string reason = "")
        {
            PlayersToRemove.Add(client);
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public override void Update()
        {
            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    SendPacketToAll(new ChatMessageGlobalPacket { Origin = -1, Message = $"Player {playerToAdd.Name} joined the game!" });

                    Server.NotifyClientConnected(this, playerToAdd);
                }
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Clients.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

                if (playerToRemove.ID != 0)
                {
                    SendPacketToAll(new DestroyPlayerPacket { Origin = -1, PlayerID = playerToRemove.ID });

                    SendPacketToAll(new ChatMessageGlobalPacket { Origin = -1, Message = $"Player {playerToRemove.Name} disconnected!" });

                    Server.NotifyClientDisconnected(this, playerToRemove);
                }

                playerToRemove.Dispose();
            }

            #endregion Player Filtration


            #region Player Updating

            // Update actual players
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();

            // Update joining players
            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i]?.Update();

            #endregion Player Updating


            if (UpdateWatch.ElapsedMilliseconds > 1000)
            {
                SendPacketToAll(new WorldDataPacket {Origin = -1, DataItems = Server.World.GenerateDataItems()});

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
        }


        public override void ClientConnected(Client client)
        {
            SendPacketToAll(new CreatePlayerPacket { Origin = -1, PlayerID = client.ID });
            var packet = client.GetDataPacket();
            packet.Origin = client.ID;
            SendPacketToAll(packet);
            SendPacketToAll(new ChatMessageGlobalPacket { Origin = -1, Message = $"Player {client.Name} joined the game!" });
        }
        public override void ClientDisconnected(Client client)
        {
            SendPacketToAll(new DestroyPlayerPacket { Origin = -1, PlayerID = client.ID });
            SendPacketToAll(new ChatMessageGlobalPacket { Origin = -1, Message = $"Player {client.Name} disconnected!" });
        }

        public override void SendPacketToAll(Packet packet)
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.SendPacket(packet);
        }

        public override void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeOffer(this, sender, monster, destClient);

            if (destClient is P3DPlayer)
                destClient.SendPacket(new TradeOfferPacket { Origin = sender.ID, DataItems = monster.ToDataItems() });
        }
        public override void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeConfirm(this, destClient, sender);

            if (destClient is P3DPlayer)
                destClient.SendPacket(new TradeStartPacket { Origin = sender.ID });
        }
        public override void SendTradeCancel(Client sender, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeCancel(this, sender, destClient);

            if (destClient is P3DPlayer)
                destClient.SendPacket(new TradeQuitPacket { Origin = sender.ID });
        }

        public override void SendPosition(Client sender, bool fromServer = false)
        {
            if(!fromServer)
                Server.NotifyClientPosition(this, sender);

            var packet = sender.GetDataPacket();
            packet.Origin = sender.ID;
            SendPacketToAll(packet);
        }


        public bool ClientPasswordCorrect(Client client, string passwordHash)
        {
            ClientTable table;
            if ((table = Server.DatabaseLoad<ClientTable>(client.ID)) != null)
                return table.PasswordHash == passwordHash;
            return false;
        }
        public bool ClientPasswordChange(Client client, string oldPassword, string newPassword)
        {
            if (client.PasswordHash == oldPassword)
            {
                client.PasswordHash = newPassword;

                Server.DatabaseSave(new ClientTable(client));

                return true;
            }

            return false;
        }

        public bool SetClientId(Client client)
        {
            if (!Server.DatabaseSetClientId(client))
            {
                RemoveClient(client, "You are already on server!");
                return false;
            }
            return true;
        }

        private void SavePlayerGJ(P3DPlayer client)
        {
            if (client != null)
            {
                var obj = new ClientGJTable(client.ID, (int) client.GameJoltID);
                if (!Server.DatabaseFind<ClientGJTable>(obj.ClientId))
                    Server.DatabaseSave(obj);
                else
                    Server.DatabaseUpdate(obj);
            }
        }
        private void LoadPlayerGJ(P3DPlayer client)
        {
            if (client != null)
            {
                var obj = new ClientGJTable(client.ID, (int) client.GameJoltID);
                if (!Server.DatabaseFind<ClientGJTable>(obj.ClientId))
                    Server.DatabaseSave(obj);
                else
                    Server.DatabaseUpdate(obj);
            }
        }


        private bool IsGameJoltIDUsed(P3DPlayer client)
        {
            for (var i = 0; i < Clients.Count; i++)
            {
                var player = Clients[i] as P3DPlayer;
                if (player.IsGameJoltPlayer && client.GameJoltID == player.GameJoltID)
                    return true;
            }

            return false;
        }

        
        public override void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();

            for (var i = 0; i < Clients.Count; i++)
            {
                Clients[i].Kick("Closing server!");
                Clients[i].Dispose();
            }
            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].Kick("Closing server!");
                PlayersToAdd[i].Dispose();
            }
            for (var i = 0; i < PlayersToRemove.Count; i++)
                PlayersToRemove[i].Dispose();
            

            Clients.Clear();
            PlayersJoining.Clear();
            PlayersToAdd.Clear();
            PlayersToRemove.Clear();


            NearPlayers.Clear();
        }
    }
}