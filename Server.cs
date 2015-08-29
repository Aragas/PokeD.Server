using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Wrappers;

using PokeD.Server.Data;

namespace PokeD.Server
{
    public class Server : IUpdatable, IDisposable
    {
        public ushort Port { get; set; }

        public float ProtocolVersion { get { return 0.5f; } }
        public string ServerName { get { return "PokeD Server"; } }
        public string ServerMessage { get { return "Testin' shit"; } }
        public int MaxPlayers { get { return 1000; } }

        public INetworkTCPServer Listener { get; set; }


        public List<Player> PlayersJoining { get; set; }
        public List<Player> Players { get; set; }
        public List<Player> PlayersToUpdate { get; set; }
        public List<Player> PlayersToAdd { get; set; }
        public List<Player> PlayersToRemove { get; set; }

        private List<NearPlayers> NearPlayersList { get; set; }

        ConcurrentQueue<PlayerPacket> PacketsToPlayer { get; set; }
        ConcurrentQueue<OriginPacket> PacketsToAllPlayers { get; set; }

        private int FreePlayerID { get; set; }

        public World World { get; set; }
        
        public Server(ushort port = 15124)
        {
            Port = port;

            PlayersJoining = new List<Player>();
            Players = new List<Player>();
            PlayersToUpdate = new List<Player>();
            PlayersToAdd = new List<Player>();
            PlayersToRemove = new List<Player>();

            PacketsToPlayer = new ConcurrentQueue<PlayerPacket>();
            PacketsToAllPlayers = new ConcurrentQueue<OriginPacket>();

            World = new World();

            NearPlayersList = new List<NearPlayers>(); 

            FreePlayerID = 10;

            Start();
        }

        public void Start()
        {
            ThreadWrapper.StartThread(ListenToClientsCycle,     true,   "ClientListnerThread");
            ThreadWrapper.StartThread(World.Update,             true,   "WorldProcessorThread");

            ThreadWrapper.StartThread(PlayerWatcher,            true, "PlayerWatcherThread");
        }



        public static int ClientListnerThreadTime { get; set; }
        private void ListenToClientsCycle()
        {
            Listener = NetworkTCPServerWrapper.NewInstance(Port);
            Listener.Start();

            var watch = Stopwatch.StartNew();
            while (true)
            {
                PlayersJoining.Add(new Player(Listener.AcceptNetworkTCPClient(), this));

                if (watch.ElapsedMilliseconds < 250)
                {
                    var time = (int) (250 - watch.ElapsedMilliseconds);
                    if (time < 0)
                        time = 0;

                    ClientListnerThreadTime = (int) watch.ElapsedMilliseconds;
                    Task.Delay(time).Wait();
                }

                watch.Reset();
                watch.Start();
            }
        }
        

        public static int PlayerWatcherThreadTime { get; set; }
        private void PlayerWatcher()
        {
            var watch = Stopwatch.StartNew();
            while (true)
            {
                var players = new List<Player>(Players);

                foreach (var player in players)
                    if (!NearPlayersList.Exists(nearPlayers => nearPlayers.LevelName == player.LevelFile))
                        NearPlayersList.Add(new NearPlayers(player.LevelFile, null));
                

                for (int i = 0; i < NearPlayersList.Count; i++)
                {
                    var nearPlayers = NearPlayersList[i];

                    var playerList = new List<Player>();
                    foreach (Player player in players)
                        if (nearPlayers.LevelName == player.LevelFile)
                            playerList.Add(player);
                    nearPlayers.Players = playerList.ToArray();

                    NearPlayersList[i] = nearPlayers;
                }

                if (watch.ElapsedMilliseconds < 4000)
                {
                    var time = (int)(400 - watch.ElapsedMilliseconds);
                    if (time < 0)
                        time = 0;

                    PlayerWatcherThreadTime = (int)watch.ElapsedMilliseconds;
                    Task.Delay(time).Wait();
                }
                watch.Reset();
                watch.Start();
            }

        }
        


        public void AddPlayer(Player player)
        {
            foreach (var online in Players)
            {
                if (player != online)
                {
                    player.SendPacketCustom(new CreatePlayerPacket { DataItems = new DataItems(online.ID.ToString()) });
                    player.SendPacketCustom(new GameDataPacket { DataItems = new DataItems(online.GeneratePlayerData()) }, online.ID);
                }
            }


            SendToAllPlayers(new CreatePlayerPacket { DataItems = new DataItems(player.ID.ToString()) });
            SendToAllPlayers(new GameDataPacket { DataItems = new DataItems(player.GeneratePlayerData()) }, player.ID);

            PlayersToAdd.Add(player);
            PlayersJoining.Remove(player);
        }

        public void RemovePlayer(Player player)
        {
            PlayersToRemove.Add(player);
        }


        public void SendToAllPlayers(IPacket packet, int originID = -1)
        {
            PacketsToAllPlayers.Enqueue(new OriginPacket(originID, ref packet));
        }



        public void Update()
        {

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
            for (int i = 0; i < PlayersToUpdate.Count; i++)
                PlayersToUpdate[i].Update();

            #endregion Player Updating



            #region Packet Sending

            PlayerPacket playerPacket;
            while (PacketsToPlayer.TryDequeue(out playerPacket))
                playerPacket.Player.SendPacket(playerPacket.Packet);

            OriginPacket originPacket;
            while (PacketsToAllPlayers.TryDequeue(out originPacket))
                foreach (var player in Players)
                    player.SendPacketCustom(originPacket.Packet, originPacket.Origin);

            #endregion Packet Sending



            #region Moving Correction

            for (int i = 0; i < NearPlayersList.Count; i++)
            {
                var players = NearPlayersList[i];
                if (players.Players != null)
                    for (int index = 0; index < players.Players.Length; index++)
                        players.Players[index].SendGameDataToOtherPlayers(players.Players);
            }

            #endregion Moving Correction

        }


        public int GeneratePlayerID()
        {
            return FreePlayerID++;
        }


        public void Dispose()
        {
            
        }




        public void ExecuteClientCommand(string message)
        {

        }

        /// <summary>
        /// Returns true if Player is muted.
        /// </summary>
        /// <param name="id">Player ID.</param>
        public bool PlayerIsMuted(int id)
        {
            return false;
        }

        /// <summary>
        /// Get Player name by ID.
        /// </summary>
        /// <param name="name">Player name.</param>
        /// <returns>Returns string.Empty is player is not found.</returns>
        public int PlayerID(string name)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.Name == name)
                    return player.ID;
            }

            return -1;
        }

        /// <summary>
        /// Get Player ID by name.
        /// </summary>
        /// <param name="id">Player ID.</param>
        /// <returns>Returns -1 is player is not found.</returns>
        public string GetPlayerName(int id)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.ID == id)
                    return player.Name;
            }

            return string.Empty;
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

        public void SendToPlayer(int destinationID, IPacket packet, int originID = -1)
        {
            var player = GetPlayer(destinationID);
            
            if(player != null)
                player.SendPacketCustom(packet, originID);
        }


        private struct PlayerPacket
        {
            public Player Player;
            public IPacket Packet;

            public PlayerPacket(Player player, ref IPacket packet)
            {
                Player = player;
                Packet = packet;
            }
        }

        private struct OriginPacket
        {
            public int Origin;
            public IPacket Packet;

            public OriginPacket(int origin, ref IPacket packet)
            {
                Origin = origin;
                Packet = packet;
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
