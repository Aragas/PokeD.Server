using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Newtonsoft.Json;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Wrappers;

using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Extensions;

namespace PokeD.Server
{
    public partial class Server : IUpdatable, IDisposable
    {
        public const string FileName = "Server.json";

        [JsonProperty("Port")]
        public ushort Port { get; private set; }

        [JsonIgnore]
        public float ProtocolVersion => 0.5f;

        [JsonProperty("ServerName")]
        public string ServerName { get; private set; }

        [JsonProperty("ServerMessage")]
        public string ServerMessage { get; private set; }

        [JsonProperty("MaxPlayers")]
        public int MaxPlayers { get; private set; }


        [JsonProperty("RemotePort")]
        public ushort RemotePort { get; private set; }

        [JsonProperty("MoveCorrectionEnabled")]
        public bool MoveCorrectionEnabled { get; private set; }

        [JsonProperty("World")]
        World World { get; set; }
        [JsonProperty("CustomWorldEnabled")]
        public bool CustomWorldEnabled { get; private set; }


        INetworkTCPServer PlayerListener { get; set; }
        INetworkTCPServer RemoteListener { get; set; }


        #region Player Stuff

        [JsonIgnore]
        public int PlayersCount => Players.Count;

        ClientList Players { get; }
        List<IClient> PlayersJoining { get; }
        List<IClient> PlayersToAdd { get; }
        List<IClient> PlayersToRemove { get; }

        ConcurrentDictionary<string, Player[]> NearPlayers { get; }

        ConcurrentQueue<PlayerPacket> PacketsToPlayer { get; set; }
        ConcurrentQueue<OriginPacket> PacketsToAllPlayers { get; set; }

        [JsonProperty("MutedPlayers")]
        Dictionary<int, List<int>> MutedPlayers { get; }

        int FreePlayerID { get; set; }

        #endregion Player Stuff

        List<RemoteClient> RemoteClients { get; }


        int ListenToConnectionsThread { get; set; }
        int PlayerWatcherThread { get; set; }
        int PlayerCorrectionThread { get; set; }

        bool IsDisposing { get; set; }


        public Server(ushort port = 15124, ushort remotePort = 15050)
        {
            ServerName = "Put name here";
            ServerMessage = "Put description here";
            MaxPlayers = 1000;
            RemotePort = 0;
            MoveCorrectionEnabled = true;
            CustomWorldEnabled = true;

            Port = port;
            RemotePort = remotePort;

            Players = new ClientList();
            PlayersJoining = new List<IClient>();
            PlayersToAdd = new List<IClient>();
            PlayersToRemove = new List<IClient>();

            PacketsToPlayer = new ConcurrentQueue<PlayerPacket>();
            PacketsToAllPlayers = new ConcurrentQueue<OriginPacket>();

            World = new World();

            NearPlayers = new ConcurrentDictionary<string, Player[]>();

            MutedPlayers = new Dictionary<int, List<int>>();

            RemoteClients = new List<RemoteClient>();

            FreePlayerID = 10;
        }

        public bool Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);

            Logger.Log(LogType.Info, $"Starting {ServerName}");

            ListenToConnectionsThread = ThreadWrapper.StartThread(ListenToConnectionsCycle, true, "ListenToConnectionsThread");
            if (MoveCorrectionEnabled)
            {
                PlayerWatcherThread = ThreadWrapper.StartThread(PlayerWatcherCycle, true, "PlayerWatcherThread");
                PlayerCorrectionThread = ThreadWrapper.StartThread(PlayerCorrectionCycle, true, "PlayerCorrectionThread");
            }

            return status;
        }

        public bool Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);

            Logger.Log(LogType.Info, $"Stopping {ServerName}");

            if(ThreadWrapper.IsRunning(ListenToConnectionsThread))
                ThreadWrapper.AbortThread(ListenToConnectionsThread);

            if (ThreadWrapper.IsRunning(PlayerWatcherThread))
                ThreadWrapper.AbortThread(PlayerWatcherThread);

            if (ThreadWrapper.IsRunning(PlayerCorrectionThread))
                ThreadWrapper.AbortThread(PlayerCorrectionThread);

            Dispose();

            return status;
        }


        public static long ClientConnectionsThreadTime { get; private set; }
        private void ListenToConnectionsCycle()
        {
            PlayerListener = NetworkTCPServerWrapper.NewInstance(Port);
            PlayerListener.Start();

            if (RemotePort != 0)
            {
                RemoteListener = NetworkTCPServerWrapper.NewInstance(RemotePort);
                RemoteListener.Start();
            }

            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
            {
                if (PlayerListener.AvailableClients)
                    PlayersJoining.Add(new Player(PlayerListener.AcceptNetworkTCPClient(), this));

                if (RemoteListener != null && RemoteListener.AvailableClients)
                    if (RemoteListener.AvailableClients)
                        RemoteClients.Add(new RemoteClient(RemoteListener.AcceptNetworkTCPClient(), this));



                if (watch.ElapsedMilliseconds < 250)
                {
                    ClientConnectionsThreadTime = watch.ElapsedMilliseconds;

                    var time = (int)(250 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    ThreadWrapper.Sleep(time);
                }

                watch.Reset();
                watch.Start();
            }
        }


        public static long PlayerWatcherThreadTime { get; private set; }
        private void PlayerWatcherCycle()
        {
            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
            {
                var players = new List<Player>(Players.GetConcreteTypeEnumerator<Player>());

                foreach (var player in players.Where(player => !NearPlayers.ContainsKey(player.LevelFile)))
                    NearPlayers.TryAdd(player.LevelFile, null);

                foreach (var level in NearPlayers.Keys)
                {
                    var playerList = new List<Player>();
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
                    foreach (var player in nearPlayers.Value.Where(player => player.IsMoving))
                        foreach (var playerToSend in nearPlayers.Value.Where(playerToSend => player != playerToSend))
                            playerToSend.SendPacket(new GameDataPacket { DataItems = player.GenerateDataItems() }, player.ID);



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



        public void AddPlayer(Player player)
        {
            FileSystemWrapperExtensions.LoadUserSettings(player);

            player.ID = GeneratePlayerID();
            SendToPlayer(player, new IDPacket { PlayerID = player.ID }, -1);
            SendToPlayer(player, new WorldDataPacket { DataItems = World.GenerateDataItems() }, -1);

            // Send to player his ID
            SendToPlayer(player, new CreatePlayerPacket { PlayerID = player.ID }, -1);
            // Send to player all Players ID
            for (var i = 0; i < Players.Count; i++)
            {
                SendToPlayer(player, new CreatePlayerPacket { PlayerID = Players[i].ID }, -1);
                SendToPlayer(player, new GameDataPacket{ DataItems = Players[i].GenerateDataItems() }, Players[i].ID);
            }
            // Send to Players player ID
            SendToAllPlayers(new CreatePlayerPacket { PlayerID = player.ID }, -1);
            SendToAllPlayers(new GameDataPacket { DataItems = player.GenerateDataItems() }, player.ID);


            PlayersToAdd.Add(player);
            PlayersJoining.Remove(player);
        }

        public void RemovePlayer(Player player)
        {
            FileSystemWrapperExtensions.SaveUserSettings(player);

            PlayersToRemove.Add(player);
        }


        public void SendToPlayer(int destinationID, IPacket packet, int originID)
        {
            SendToPlayer(GetPlayer(destinationID), packet, originID);
        }

        public void SendToPlayer(IClient player, IPacket packet, int originID)
        {
            if (player != null)
                PacketsToPlayer.Enqueue(new PlayerPacket(player, ref packet, originID));
        }

        public void SendToAllPlayers(IPacket packet, int originID = -1)
        {
            if (originID != -1 && (packet is ChatMessagePacket || packet is ChatMessagePrivatePacket))
                if (MutedPlayers.ContainsKey(originID) && MutedPlayers[originID].Count > 0)
                {
                    for (int i = 0; i < Players.Count; i++)
                    {
                        var player = Players[i];
                        if (!MutedPlayers[originID].Contains(player.ID))
                            PacketsToPlayer.Enqueue(new PlayerPacket(player , ref packet, originID));
                    }

                    return;
                }

            PacketsToAllPlayers.Enqueue(new OriginPacket(ref packet, originID));
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            for (var i = 0; RemoteListener != null && i < RemoteClients.Count; i++)
                RemoteClients[i].Update();

            
            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Players.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    Logger.Log(LogType.Server, $"The player {playerToAdd.Name} joined the game from IP {playerToAdd.IP}");
                    SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems($"Player {playerToAdd.Name} joined the game!") });
                }
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Players.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

                if (playerToRemove.ID != 0)
                {
                    SendToAllPlayers(new DestroyPlayerPacket { DataItems = new DataItems(playerToRemove.ID.ToString()) });

                    Logger.Log(LogType.Server, $"The player {playerToRemove.Name} disconnected, playtime was {DateTime.Now - playerToRemove.ConnectionTime:hh\\:mm\\:ss}");
                    SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems($"Player {playerToRemove.Name} disconnected!") });
                }
            }

            #endregion Player Filtration



            #region Player Updating

            // Add actual players
            for (var i = 0; i < Players.Count; i++)
                Players[i].Update();

            // Add joining players
            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Update();
            
            #endregion Player Updating



            #region Packet Sending

            PlayerPacket playerPacket;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out playerPacket))
                playerPacket.Player.SendPacket(playerPacket.Packet, playerPacket.OriginID);

            OriginPacket originPacket;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out originPacket))
                for (var i = 0; i < Players.Count; i++)
                    Players[i].SendPacket(originPacket.Packet, originPacket.OriginID);

            #endregion Packet Sending



            //for (int i = 0; i < NearPlayersList.Count; i++)
            //{
            //    var nearPlayer = NearPlayersList[i];
            //    foreach (var player in nearPlayer.Players)
            //        player.SendGameDataPlayers(nearPlayer.Players);
            //}



            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (!player.UseCustomWorld)
                    SendToPlayer(player, new WorldDataPacket { DataItems = World.GenerateDataItems() }, -1);
            }

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        public int GeneratePlayerID()
        {
            return FreePlayerID++;
        }



        public MuteStatus MutePlayer(int id, string muteName)
        {
            if (!MutedPlayers.ContainsKey(id))
                MutedPlayers.Add(id, new List<int>());

            var muteID = GetPlayerID(muteName);
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

            var muteID = GetPlayerID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Remove(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.PlayerNotFound;
        }


        public void SendServerMessageToAll(string message)
        {
            for (int i = 0; i < Players.Count; i++)
                Players[i].SendPacket(new ServerMessagePacket { Message = message }, -1);
        }

        public void SendGlobalChatMessageToAll(string message)
        {
            for (int i = 0; i < Players.Count; i++)
                Players[i].SendPacket(new ChatMessagePacket { Message = message }, -1);
        }


        public string[] GetPlayerNames()
        {
            var list = new List<string>();

            for (var i = 0; i < Players.Count; i++)
                list.Add(Players[i].Name);

            return list.ToArray();
        }
            
        /// <summary>
        /// Get Player ID by name.
        /// </summary>
        /// <param name="id">Player ID.</param>
        /// <returns>Returns -1 is player is not found.</returns>
        public string GetPlayerName(int id)
        {
            var player = GetPlayer(id);
            return player != null ? player.Name : string.Empty;
        }

        /// <summary>
        /// Get Player class by ID.
        /// </summary>
        /// <param name="id">Player ID.</param>
        /// <returns>Returns null is player is not found.</returns>
        public IClient GetPlayer(int id)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.ID == id)
                    return player;
            }

            return null;
        }

        /// <summary>
        /// Get Player name by ID.
        /// </summary>
        /// <param name="name">Player name.</param>
        /// <returns>Returns string.Empty is player is not found.</returns>
        public int GetPlayerID(string name)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.Name == name)
                    return player.ID;
            }

            return -1;
        }


        public void RemoveRemoteClient(RemoteClient remoteClient)
        {
            RemoteClients.Remove(remoteClient);
        }
        

        public void Dispose()
        {
            IsDisposing = true;

            for (int i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();

            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].SendPacket(new ServerClosePacket { Reason = "Closing server" }, -1);
                Players[i].Dispose();
            }
            for (int i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new ServerClosePacket { Reason = "Closing server" }, -1);
                PlayersToAdd[i].Dispose();
            }

            // Do not dispose PlayersToRemove!


            Players?.Clear();
            PlayersJoining?.Clear();
            PlayersToAdd?.Clear();
            PlayersToRemove?.Clear();

            if (PacketsToPlayer != null)
                PacketsToPlayer = null;

            if (PacketsToAllPlayers != null)
                PacketsToAllPlayers = null;

            World?.Dispose();

            NearPlayers?.Clear();

            MutedPlayers?.Clear();
        }
    }
}
