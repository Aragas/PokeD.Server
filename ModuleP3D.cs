using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Packets;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Trade;

using PokeD.Server.Clients;
using PokeD.Server.Clients.P3D;
using PokeD.Server.Extensions;

namespace PokeD.Server
{
    public enum MuteStatus
    {
        Completed,
        PlayerNotFound,
        MutedYourself,
        IsNotMuted
    }

    public class ModuleP3D : IServerModule
    {
        const string FileName = "ModuleP3D.json";

        #region Settings

        [JsonProperty("Enabled")]
        public bool Enabled { get; private set; } = false;

        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 15124;

        [JsonProperty("ServerName", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerName { get; private set; } = "Put name here";

        [JsonProperty("ServerMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerMessage { get; private set; } = "Put description here";
        
        [JsonProperty("MaxPlayers")]
        public int MaxPlayers { get; private set; } = 1000;

        [JsonProperty("EncryptionEnabled")]
        public bool EncryptionEnabled { get; private set; } = true;

        [JsonProperty("MoveCorrectionEnabled")]
        public bool MoveCorrectionEnabled { get; private set; } = true;

        [JsonProperty("MutedPlayers")]
        public Dictionary<int, List<int>> MutedPlayers { get; } = new Dictionary<int, List<int>>();

        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }
        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        IThread PlayerWatcherThread { get; set; }
        IThread PlayerCorrectionThread { get; set; }


        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        [JsonIgnore]
        public bool ClientsVisible { get; } = true;

        List<IClient> PlayersJoining { get; } = new List<IClient>();
        List<IClient> PlayersToAdd { get; } = new List<IClient>();
        List<IClient> PlayersToRemove { get; } = new List<IClient>();


        ConcurrentQueue<PlayerPacketP3DOrigin> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketP3DOrigin>();
        ConcurrentQueue<PacketP3DOrigin> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<PacketP3DOrigin>();

        ConcurrentDictionary<string, P3DPlayer[]> NearPlayers { get; } = new ConcurrentDictionary<string, P3DPlayer[]>();


        public ModuleP3D(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
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
                PlayerWatcherThread = ThreadWrapper.CreateThread(PlayerWatcherCycle);
                PlayerWatcherThread.Name = "PlayerWatcherThread";
                PlayerWatcherThread.IsBackground = true;
                PlayerWatcherThread.Start();

                PlayerCorrectionThread = ThreadWrapper.CreateThread(PlayerCorrectionCycle);
                PlayerCorrectionThread.Name = "PlayerCorrectionThread";
                PlayerCorrectionThread.IsBackground = true;
                PlayerCorrectionThread.Start();
            }

            return true;
        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
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
                //var players = new List<IClient>(Clients.GetConcreteTypeEnumerator<P3DPlayer, ProtobufPlayer>());

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
                    ThreadWrapper.Sleep(time);
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
                            playerToSend.SendPacket(player.GetDataPacket(), player.ID);

                /*
                for (int i = 0; i < Players.Count; i++)
                {
                    var player1 = Players[i];
                    for (int j = 0; j < Players.Count; j++)
                    {
                        var player2 = Players[j];

                        if(player1 != player2)
                            if (player1.IsMoving)
                                SendToClient(player2, player1.GetDataPacket(), player1.ID);
                        //var nearPlayer = Players[i];
                        //if (nearPlayer.IsMoving)
                        //    SendToAllClients(nearPlayer.GetDataPacket(), nearPlayer.ID);
                    }
                    //if (nearPlayer.IsMoving)
                    //    SendToAllClients(nearPlayer.GetDataPacket(), nearPlayer.ID);
                }
                */


                if (watch.ElapsedMilliseconds < 5)
                {
                    PlayerCorrectionThreadTime = watch.ElapsedMilliseconds;

                    var time = (int)(5 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    ThreadWrapper.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }
        }


        public void StartListen()
        {
            Listener = TCPListenerWrapper.CreateTCPListener(Port);
            Listener.Start();
        }
        public void CheckListener()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    PlayersJoining.Add(new P3DPlayer(Listener.AcceptTCPClient(), this));
        }


        public void PreAdd(IClient client)
        {
            if (Server.PeekDBID(client) != -1)
            {
                P3DPlayerSendToClient(client, new IDPacket { PlayerID = client.ID }, -1);
                P3DPlayerSendToClient(client, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);
            }
        }
        public void AddClient(IClient client)
        {
            if (IsGameJoltIDUsed(client as P3DPlayer))
            {
                RemoveClient(client, "You are already on server!");
                return;
            }

            if (!Server.LoadDBPlayer(client))
            {
                RemoveClient(client, "Wrong password or you are already on server!");
                return;
            }

            P3DPlayerSendToClient(client, new IDPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToClient(client, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);

            // Send to player his ID
            P3DPlayerSendToClient(client, new CreatePlayerPacket { PlayerID = client.ID }, -1);
            // Send to player all Players ID
            foreach (var aClient in Server.AllClients())
            {
                P3DPlayerSendToClient(client, new CreatePlayerPacket { PlayerID = aClient.ID }, -1);
                P3DPlayerSendToClient(client, aClient.GetDataPacket(), aClient.ID);
            }
            // Send to Players player ID
            P3DPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToAllClients(client.GetDataPacket(), client.ID);


            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);
        }
        public void RemoveClient(IClient client, string reason = "")
        {
            if (!string.IsNullOrEmpty(reason))
                client.SendPacket(new KickedPacket { Reason = reason }, -1);

            PlayersToRemove.Add(client);
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToAdd.Name} joined the game!" });

                    Server.ClientConnected(this, playerToAdd);
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
                    P3DPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = playerToRemove.ID });

                    P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToRemove.Name} disconnected!" });

                    Server.ClientDisconnected(this, playerToRemove);
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


            #region Packet Sending

            PlayerPacketP3DOrigin packetToPlayer;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out packetToPlayer))
                packetToPlayer.Player.SendPacket(packetToPlayer.Packet, packetToPlayer.OriginID);

            PacketP3DOrigin packetToAllPlayers;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out packetToAllPlayers))
                for (var i = 0; i < Clients.Count; i++)
                    Clients[i].SendPacket(packetToAllPlayers.Packet, packetToAllPlayers.OriginID);

            #endregion Packet Sending



            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            for (var i = 0; i < Clients.Count; i++)
            {
                var player = Clients[i];
                if (player == null) continue;

                P3DPlayerSendToClient(player, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);
            }

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        public void OtherConnected(IClient client)
        {
            P3DPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToAllClients(client.GetDataPacket(), client.ID);
            P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {client.Name} joined the game!" });
        }
        public void OtherDisconnected(IClient client)
        {
            P3DPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = client.ID });
            P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {client.Name} disconnected!" });
        }


        public void SendServerMessage(IClient sender, string message)
        {
            if (sender is P3DPlayer)
            {
                P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = message }, -1);
                Server.ClientServerMessage(this, sender, message);
            }
            else
                P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = message }, -1);
        }
        public void SendPrivateMessage(IClient sender, IClient destClient, string message)
        {
            if (destClient is P3DPlayer)
                P3DPlayerSendToClient(destClient, new ChatMessagePrivatePacket { DataItems = new DataItems(message) }, sender.ID);
            else
                Server.ClientPrivateMessage(this, sender, destClient, message);
        }
        public void SendGlobalMessage(IClient sender, string message)
        {
            if (sender is P3DPlayer)
            {
                P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = message }, sender.ID);
                Server.ClientGlobalMessage(this, sender, message);
            }
            else
                P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = message }, sender.ID);
        }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient)
        {
            if (destClient is P3DPlayer)
            {
                P3DPlayerSendToClient(destClient, new TradeRequestPacket(), sender.ID);
                P3DPlayerSendToClient(destClient, new TradeOfferPacket() { DataItems = monster.ToDataItems() }, sender.ID);
            }
            else
                Server.ClientTradeOffer(this, sender, monster, destClient);
        }
        public void SendTradeConfirm(IClient sender, IClient destClient)
        {
            if (destClient is P3DPlayer)
                P3DPlayerSendToClient(destClient, new TradeStartPacket(), sender.ID);
            else
                Server.ClientTradeConfirm(this, destClient, sender);
        }
        public void SendTradeCancel(IClient sender, IClient destClient)
        {
            if (destClient is P3DPlayer)
                P3DPlayerSendToClient(destClient, new TradeQuitPacket(), sender.ID);
            else
                Server.ClientTradeCancel(this, sender, destClient);
        }

        public void SendPosition(IClient sender)
        {
            if (sender is P3DPlayer)
            {
                Server.ClientPosition(this, sender);
                P3DPlayerSendToAllClients(sender.GetDataPacket(), sender.ID);
            }
            else
                P3DPlayerSendToAllClients(sender.GetDataPacket(), sender.ID);
        }


        public void P3DPlayerSendToClient(int destinationID, P3DPacket packet, int originID)
        {
            var player = Server.GetClient(destinationID);
            if (player != null)
                P3DPlayerSendToClient(player, packet, originID);
        }
        public void P3DPlayerSendToClient(IClient player, P3DPacket packet, int originID)
        {
            if (packet is TradeRequestPacket)
            {
                var client = Server.GetClient(originID);
                if(player.GetType() != typeof(P3DPlayer))
                    P3DPlayerSendToClient(client, new TradeJoinPacket(), player.ID);
            }
            if (packet is TradeStartPacket)
            {
                var client = Server.GetClient(originID);
                if (player.GetType() != typeof(P3DPlayer))
                    P3DPlayerSendToClient(client, new TradeStartPacket(), player.ID);
            }

            PacketsToPlayer.Enqueue(new PlayerPacketP3DOrigin(player, ref packet, originID));
        }
        public void P3DPlayerSendToAllClients(P3DPacket packet, int originID = -1)
        {
            if (originID != -1 && (packet is ChatMessageGlobalPacket || packet is ChatMessagePrivatePacket))
                if (MutedPlayers.ContainsKey(originID) && MutedPlayers[originID].Count > 0)
                {
                    for (var i = 0; i < Clients.Count; i++)
                    {
                        var player = Clients[i];
                        if (!MutedPlayers[originID].Contains(player.ID))
                            PacketsToPlayer.Enqueue(new PlayerPacketP3DOrigin(player, ref packet, originID));
                    }

                    return;
                }

            PacketsToAllPlayers.Enqueue(new PacketP3DOrigin(ref packet, originID));
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

        public MuteStatus MutePlayer(int id, string muteName)
        {
            if (!MutedPlayers.ContainsKey(id))
                MutedPlayers.Add(id, new List<int>());

            var muteID = Server.GetClientID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Add(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.PlayerNotFound;
        }
        public MuteStatus UnMutePlayer(int id, string muteName)
        {
            if (!MutedPlayers.ContainsKey(id))
                return MuteStatus.IsNotMuted;

            var muteID = Server.GetClientID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Remove(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.PlayerNotFound;
        }

        public void P3DPlayerChangePassword(IClient client, string oldPassword, string newPassword)
        {
            if (client.PasswordHash == oldPassword)
                client.PasswordHash = newPassword;

            Server.UpdateDBPlayer(client, true);
        }



        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();

            for (var i = 0; i < Clients.Count; i++)
            {
                Clients[i].SendPacket(new ServerClosePacket { Reason = "Closing server!" }, -1);
                Clients[i].Dispose();
            }
            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new ServerClosePacket { Reason = "Closing server!" }, -1);
                PlayersToAdd[i].Dispose();
            }

            // Do not dispose PlayersToRemove!


            Clients.Clear();
            PlayersJoining.Clear();
            PlayersToAdd.Clear();
            PlayersToRemove.Clear();

            PacketsToPlayer = null;
            PacketsToAllPlayers = null;

            NearPlayers.Clear();

            MutedPlayers.Clear();
        }

        

        private class PlayerPacketP3DOrigin
        {
            public readonly IClient Player;
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public PlayerPacketP3DOrigin(IClient player, ref P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
            public PlayerPacketP3DOrigin(IClient player, P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        } 
        private class PacketP3DOrigin
        {
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public PacketP3DOrigin(ref P3DPacket packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}
