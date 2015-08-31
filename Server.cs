using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Newtonsoft.Json;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Wrappers;

using PokeD.Server.Data;
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

    public class Server : IUpdatable, IDisposable
    {
        public const string FileName = "Server.json";

        [JsonProperty("Port")]
        public ushort Port { get; private set; }

        [JsonIgnore]
        public float ProtocolVersion { get { return 0.5f; } }

        [JsonProperty("ServerName")]
        public string ServerName { get { return "PokeD Server"; } }

        [JsonProperty("ServerMessage")]
        public string ServerMessage { get; set; }

        [JsonProperty("MaxPlayers")]
        public int MaxPlayers { get; set; }


        [JsonProperty("RemotePort")]
        public ushort RemotePort { get; private set; }

        public World World { get; private set; }


        INetworkTCPServer PlayerListener { get; set; }
        INetworkTCPServer RemoteListener { get; set; }


        #region Player Stuff

        [JsonIgnore]
        public int PlayersCount { get { return Players.Count; } }
        List<Player> Players { get; set; }
        List<Player> PlayersJoining { get; set; }
        List<Player> PlayersToUpdate { get; set; }
        List<Player> PlayersToAdd { get; set; }
        List<Player> PlayersToRemove { get; set; }

        List<NearPlayers> NearPlayersList { get;  set; }

        ConcurrentQueue<PlayerPacket> PacketsToPlayer { get; set; }
        ConcurrentQueue<OriginPacket> PacketsToAllPlayers { get; set; }

        [JsonProperty("MutedPlayers")]
        Dictionary<int, List<int>> MutedPlayers { get; set; }
        
        int FreePlayerID { get; set; }

        #endregion Player Stuff
        
        List<RemoteClient> RemoteClients { get; set; }

        public Server(ushort port = 15124, ushort remotePort = 15050)
        {
            Port = port;
            RemotePort = remotePort;

            Players = new List<Player>();
            PlayersJoining = new List<Player>();
            PlayersToUpdate = new List<Player>();
            PlayersToAdd = new List<Player>();
            PlayersToRemove = new List<Player>();

            PacketsToPlayer = new ConcurrentQueue<PlayerPacket>();
            PacketsToAllPlayers = new ConcurrentQueue<OriginPacket>();

            World = new World();

            NearPlayersList = new List<NearPlayers>();

            MutedPlayers = new Dictionary<int, List<int>>();

            FreePlayerID = 10;
        }


        public void Start()
        {
            ThreadWrapper.StartThread(ListenToConnectionsCycle, true, "ListenToConnectionsThread");
            ThreadWrapper.StartThread(PlayerWatcherCycle,       true, "PlayerWatcherThread");
            ThreadWrapper.StartThread(PlayerCorrectionCycle,    true, "PlayerCorrectionThread");
        }

        public void Stop()
        {
            FileSystemWrapper.SaveSettings(FileName, this);

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

            Dispose();
        }


        public static long ClientConnectionsThreadTime { get; private set; }
        private void ListenToConnectionsCycle()
        {
            PlayerListener = NetworkTCPServerWrapper.NewInstance(Port);
            PlayerListener.Start();

            RemoteListener = NetworkTCPServerWrapper.NewInstance(RemotePort);
            RemoteListener.Start();

            var watch = Stopwatch.StartNew();
            while (true)
            {
                if(PlayerListener.AvailableClients)
                    PlayersJoining.Add(new Player(PlayerListener.AcceptNetworkTCPClient(), this));

                if (RemoteListener.AvailableClients)
                    RemoteClients.Add(new RemoteClient(RemoteListener.AcceptNetworkTCPClient(), this));

                if (watch.ElapsedMilliseconds < 250)
                {
                    ClientConnectionsThreadTime = watch.ElapsedMilliseconds;
                    Task.Delay((int)(250 - watch.ElapsedMilliseconds)).Wait();
                }

                watch.Reset();
                watch.Start();
            }
        }
        

        public static long PlayerWatcherThreadTime { get; private set; }
        private void PlayerWatcherCycle()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                var players = new List<Player>(Players);

                foreach (var player in players)
                    if (!NearPlayersList.Exists(nearPlayers => nearPlayers.LevelName == player.LevelFile))
                        NearPlayersList.Add(new NearPlayers(player.LevelFile, null));
                

                for (var i = 0; i < NearPlayersList.Count; i++)
                {
                    var nearPlayers = NearPlayersList[i];

                    var playerList = new List<Player>();
                    foreach (var player in players)
                        if (nearPlayers.LevelName == player.LevelFile)
                            playerList.Add(player);
                    nearPlayers.Players = playerList.ToArray();

                    NearPlayersList[i] = nearPlayers;
                }

                if (watch.ElapsedMilliseconds < 4000)
                {
                    PlayerWatcherThreadTime = watch.ElapsedMilliseconds;
                    Task.Delay((int)(400 - watch.ElapsedMilliseconds)).Wait();
                }
                watch.Reset();
                watch.Start();
            }

        }

        public static long PlayerCorrectionThreadTime { get; private set; }
        private void PlayerCorrectionCycle()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                for (int i = 0; i < NearPlayersList.Count; i++)
                {
                    var players = NearPlayersList[i];
                    if (players.Players != null)
                        for (var index = 0; index < players.Players.Length; index++)
                            if (players.Players[index].IsMoving)
                                players.Players[index].SendGameDataPlayers(players.Players);
                }


                if (watch.ElapsedMilliseconds < 16)
                {
                    PlayerCorrectionThreadTime = watch.ElapsedMilliseconds;
                    Task.Delay((int)(16 - watch.ElapsedMilliseconds)).Wait();
                }
                watch.Reset();
                watch.Start();
            }

        }



        public void AddPlayer(Player player)
        {
            FileSystemWrapperExtensions.LoadUserSettings(ref player);

            for (int i = 0; i < Players.Count; i++)
            {
                var online = Players[i];
                if (player != online)
                {
                    player.SendPacket(new CreatePlayerPacket { PlayerID = online.ID }, -1);
                    player.SendPacket(new GameDataPacket { DataItems = online.GenerateDataItems() }, online.ID);
                }
            }

            player.SendPacket(new CreatePlayerPacket { PlayerID = player.ID }, -1);


            SendToAllPlayers(new CreatePlayerPacket { PlayerID = player.ID });
            SendToAllPlayers(new GameDataPacket { DataItems = player.GenerateDataItems() }, player.ID);

            PlayersToAdd.Add(player);
            PlayersJoining.Remove(player);
        }

        public void RemovePlayer(Player player)
        {
            FileSystemWrapperExtensions.SaveUserSettings(player);

            PlayersToRemove.Add(player);

            if (player.ID != 0)
            {
                SendToAllPlayers(new DestroyPlayerPacket { DataItems = new DataItems(player.ID.ToString()) });

                SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems(string.Format("Player {0} disconnected!", player.Name)) });
            }
        }


        public void SendToPlayer(int destinationID, IPacket packet, int originID)
        {
            SendToPlayer(GetPlayer(destinationID), packet, originID);
        }

        public void SendToPlayer(Player player, IPacket packet, int originID)
        {
            if (player != null)
                PacketsToPlayer.Enqueue(new PlayerPacket(player, ref packet, originID));
        }

        public void SendToAllPlayers(IPacket packet, int originID = -1)
        {
            if (originID != -1)
                if(packet is ChatMessagePacket || packet is ChatMessagePrivatePacket)
                    if (MutedPlayers.ContainsKey(originID) && MutedPlayers[originID].Count > 0)
                    {
                        for (int i = 0; i < Players.Count; i++)
                        {
                            var player = Players[i];
                            if (!MutedPlayers[originID].Contains(player.ID))
                                PacketsToPlayer.Enqueue(new PlayerPacket(player, ref packet, originID));
                        }

                        return;
                    }


            PacketsToAllPlayers.Enqueue(new OriginPacket(ref packet, originID));
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {

            for (int i = 0; i < RemoteClients.Count; i++)
                RemoteClients[i].Update();
            


            #region Player Filtration

            for (int i = 0; i < PlayersToAdd.Count; i++)
            {
                var player = PlayersToAdd[i];
                Players.Add(player);
                PlayersToAdd.Remove(player);

                SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems(string.Format("Player {0} joined the game!", player.Name)) });
            }

            for (int i = 0; i < PlayersToRemove.Count; i++)
            {
                var player = PlayersToRemove[i];
                Players.Remove(player);
                PlayersToRemove.Remove(player);
            }

            #endregion Player Filtration



            #region Player Updating

            PlayersToUpdate.Clear();

            // Add actual players
            for (int i = 0; i < Players.Count; i++)
                PlayersToUpdate.Add(Players[i]);

            // Add joining players
            for (int i = 0; i < PlayersJoining.Count; i++)
                PlayersToUpdate.Add(PlayersJoining[i]);

            // Actual updating
            foreach (var player in PlayersToUpdate)
                player.Update();

            #endregion Player Updating



            #region Packet Sending

            PlayerPacket playerPacket;
            while (PacketsToPlayer.TryDequeue(out playerPacket))
                playerPacket.Player.SendPacket(playerPacket.Packet, playerPacket.OriginID);

            OriginPacket originPacket;
            while (PacketsToAllPlayers.TryDequeue(out originPacket))
                foreach (var player in Players)
                    player.SendPacket(originPacket.Packet, originPacket.OriginID);

            #endregion Packet Sending



            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if(!player.UseCustomWorld)
                    player.SendPacket(new WorldDataPacket { DataItems = World.GenerateDataItems() }, -1);
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


        public void ExecuteServerCommand(string command)
        {

        }

        public void SendServerMessageToAll(string message)
        {
            for (int i = 0; i < Players.Count; i++)
                Players[i].SendPacket(new ServerMessagePacket { Message = message }, -1);
        }

        public void SendGlobalChatMessageToAll(string message)
        {
            for (int i = 0; i < Players.Count; i++)
                Players[i].SendPacket(new ChatMessagePacket  { Message = message }, -1);
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
        public Player GetPlayer(int id)
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
            for (int i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();

            // Do not dispose PlayersToRemove!


            if (Players != null)
                Players.Clear();

            if (PlayersJoining != null)
                PlayersJoining.Clear();

            if (PlayersToUpdate != null)
                PlayersToUpdate.Clear();

            if (PlayersToAdd != null)
                PlayersToAdd.Clear();

            if (PlayersToRemove != null)
                PlayersToRemove.Clear();
           
            if (PacketsToPlayer != null)
                PacketsToPlayer = null;

            if (PacketsToAllPlayers != null)
                PacketsToAllPlayers = null;

            if (World != null)
                World.Dispose();

            if (NearPlayersList != null)
                NearPlayersList.Clear();

            if (MutedPlayers != null)
                MutedPlayers.Clear();
        }


        private struct PlayerPacket
        {
            public Player Player;
            public IPacket Packet;
            public int OriginID;

            public PlayerPacket(Player player, ref IPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        }
        
        private struct OriginPacket
        {
            public IPacket Packet;
            public int OriginID;

            public OriginPacket(ref IPacket packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }

        private struct NearPlayers
        {
            public string LevelName { get; set; }
            public Player[] Players { get; set; }

            public NearPlayers(string levelName, Player[] players) : this()
            {
                LevelName = levelName;
                Players = players;
            }
        }
    }
}
