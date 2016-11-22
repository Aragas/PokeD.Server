﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using PCLExt.Config.Extensions;
using PCLExt.Network;
using PCLExt.Thread;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Extensions;
using PokeD.Core.Packets.P3D;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Trade;
using PokeD.Server.Clients;
using PokeD.Server.Clients.P3D;

namespace PokeD.Server
{
    public class ModuleP3D : ServerModule
    {
        public enum MuteStatus
        {
            Completed,
            PlayerNotFound,
            MutedYourself,
            IsNotMuted
        }

        const string FileName = "ModuleP3D";

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15124;

        public string ServerName { get; protected set; } = "Put name here";

        public string ServerMessage { get; protected set; } = "Put description here";
        
        public int MaxPlayers { get; protected set; } = 1000;

        public bool EncryptionEnabled { get; protected set; } = true;

        public bool MoveCorrectionEnabled { get; protected set; } = true;

        public Dictionary<int, List<int>> MutedPlayers { get; protected set; } = new Dictionary<int, List<int>>();

        #endregion Settings

        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        IThread PlayerWatcherThread { get; set; }
        IThread PlayerCorrectionThread { get; set; }


        List<Client> PlayersJoining { get; } = new List<Client>();
        List<Client> PlayersToAdd { get; } = new List<Client>();
        List<Client> PlayersToRemove { get; } = new List<Client>();


        ConcurrentQueue<PlayerPacketP3DOrigin> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketP3DOrigin>();
        ConcurrentQueue<PacketP3DOrigin> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<PacketP3DOrigin>();

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


        public void PreAdd(Client client)
        {
            if (Server.DatabasePlayerGetID(client) != -1)
            {
                P3DPlayerSendToClient(client, new IDPacket { PlayerID = client.ID }, -1);
                P3DPlayerSendToClient(client, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);
            }
        }
        public void AddClient(Client client)
        {
            if (IsGameJoltIDUsed(client as P3DPlayer))
            {
                RemoveClient(client, "You are already on server!");
                return;
            }

            if (!Server.DatabasePlayerLoad(client))
            {
                RemoveClient(client, "Wrong password or you are already on server!");
                return;
            }

            P3DPlayerSendToClient(client, new IDPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToClient(client, new WorldDataPacket { DataItems = Server.World.GenerateDataItems() }, -1);

            // Send to player his ID
            P3DPlayerSendToClient(client, new CreatePlayerPacket { PlayerID = client.ID }, -1);
            // Send to player all Players ID
            foreach (var aClient in Server.GetAllClients())
            {
                P3DPlayerSendToClient(client, new CreatePlayerPacket { PlayerID = aClient.ID }, -1);
                P3DPlayerSendToClient(client, aClient.GetDataPacket(), aClient.ID);
            }
            // Send to Players player ID
            P3DPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToAllClients(client.GetDataPacket(), client.ID);


            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);

            //Server.ChatChannelManager.FindByAlias("global").Subscribe(client);
        }
        public void RemoveClient(Client client, string reason = "")
        {
            if (!string.IsNullOrEmpty(reason))
                client.SendPacket(new KickedPacket { Origin = -1, Reason = reason });

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
                    P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToAdd.Name} joined the game!" });

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
                    P3DPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = playerToRemove.ID });

                    P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToRemove.Name} disconnected!" });

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


            #region Packet Sending

            PlayerPacketP3DOrigin packetToPlayer;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out packetToPlayer))
            {
                packetToPlayer.Packet.Origin = packetToPlayer.OriginID;
                packetToPlayer.Player.SendPacket(packetToPlayer.Packet);
            }

            PacketP3DOrigin packetToAllPlayers;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out packetToAllPlayers))
                for (var i = 0; i < Clients.Count; i++)
                {
                    packetToAllPlayers.Packet.Origin = packetToAllPlayers.OriginID;
                    Clients[i].SendPacket(packetToAllPlayers.Packet);
                }

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


        public override void ClientConnected(Client client)
        {
            P3DPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            P3DPlayerSendToAllClients(client.GetDataPacket(), client.ID);
            P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {client.Name} joined the game!" });
        }
        public override void ClientDisconnected(Client client)
        {
            P3DPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = client.ID });
            P3DPlayerSendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {client.Name} disconnected!" });
        }


        public override void SendPrivateMessage(Client sender, Client destClient, string message, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientPrivateMessage(this, sender, destClient, message);

            if (destClient is P3DPlayer)
                P3DPlayerSendToClient(destClient, new ChatMessagePrivatePacket { DataItems = new DataItems(message) }, sender.ID);
        }


        public override void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeOffer(this, sender, monster, destClient);

            if (destClient is P3DPlayer)
                P3DPlayerSendToClient(destClient, new TradeOfferPacket { DataItems = monster.ToDataItems() }, sender.ID);
        }
        public override void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeConfirm(this, destClient, sender);

            if (destClient is P3DPlayer)
                P3DPlayerSendToClient(destClient, new TradeStartPacket(), sender.ID);
        }
        public override void SendTradeCancel(Client sender, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeCancel(this, sender, destClient);

            if (destClient is P3DPlayer)
                P3DPlayerSendToClient(destClient, new TradeQuitPacket(), sender.ID);
        }

        public override void SendPosition(Client sender, bool fromServer = false)
        {
            if(!fromServer)
                Server.NotifyClientPosition(this, sender);

            P3DPlayerSendToAllClients(sender.GetDataPacket(), sender.ID);
        }


        public void P3DPlayerSendToClient(int destinationID, P3DPacket packet, int originID)
        {
            var player = Server.GetClient(destinationID);
            if (player != null)
                P3DPlayerSendToClient(player, packet, originID);
        }
        public void P3DPlayerSendToClient(Client player, P3DPacket packet, int originID)
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

        public void P3DPlayerChangePassword(Client client, string oldPassword, string newPassword)
        {
            if (client.PasswordHash == oldPassword)
                client.PasswordHash = newPassword;

            Server.DatabasePlayerSave(client, true);
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
                Clients[i].SendPacket(new ServerClosePacket { Origin = -1, Reason = "Closing server!" });
                Clients[i].Dispose();
            }
            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new ServerClosePacket { Origin = -1, Reason = "Closing server!" });
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
            public Client Player { get; }
            public P3DPacket Packet { get; }
            public int OriginID { get; }

            public PlayerPacketP3DOrigin(Client player, ref P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
            public PlayerPacketP3DOrigin(Client player, P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        } 
        private class PacketP3DOrigin
        {
            public P3DPacket Packet { get; }
            public int OriginID { get; }

            public PacketP3DOrigin(ref P3DPacket packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}