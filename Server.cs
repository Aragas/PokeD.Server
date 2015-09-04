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
        public string ServerName { get; set; }

        [JsonProperty("ServerMessage")]
        public string ServerMessage { get; set; }

        [JsonProperty("MaxPlayers")]
        public int MaxPlayers { get; set; }


        [JsonProperty("RemotePort")]
        public ushort RemotePort { get; private set; }

        [JsonProperty("MoveCorrectionEnabled")]
        public bool MoveCorrectionEnabled { get; private set; }

        [JsonProperty("World")]
        public World World { get; private set; }
        [JsonProperty("CustomWorldEnabled")]
        public bool CustomWorldEnabled { get; private set; }


        INetworkTCPServer PlayerListener { get; set; }
        INetworkTCPServer RemoteListener { get; set; }


        #region Player Stuff

        [JsonIgnore]
        public int PlayersCount { get { return Players.Count; } }
        ClientList Players { get; set; }
        List<IClient> PlayersJoining { get; set; }
        List<IClient> PlayersToAdd { get; set; }
        List<IClient> PlayersToRemove { get; set; }

        List<NearPlayers> NearPlayersList { get; set; }

        ConcurrentQueue<PlayerPacket> PacketsToPlayer { get; set; }
        ConcurrentQueue<OriginPacket> PacketsToAllPlayers { get; set; }

        [JsonProperty("MutedPlayers")]
        Dictionary<int, List<int>> MutedPlayers { get; set; }

        int FreePlayerID { get; set; }

        #endregion Player Stuff

        List<RemoteClient> RemoteClients { get; set; }

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

            NearPlayersList = new List<NearPlayers>();

            MutedPlayers = new Dictionary<int, List<int>>();

            RemoteClients = new List<RemoteClient>();

            FreePlayerID = 10;
        }


        public bool Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);

            Logger.Log(LogType.Info, string.Format("Starting {0}", ServerName));

            ThreadWrapper.StartThread(ListenToConnectionsCycle, true, "ListenToConnectionsThread");
            if (MoveCorrectionEnabled)
            {
                ThreadWrapper.StartThread(PlayerWatcherCycle, true, "PlayerWatcherThread");
                ThreadWrapper.StartThread(PlayerCorrectionCycle, true, "PlayerCorrectionThread");
            }

            return status;
        }

        public bool Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);

            Logger.Log(LogType.Info, string.Format("Stopping {0}", ServerName));

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
            while (true)
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
            while (true)
            {
                var players = new List<Player>(Players.GetConcreteTypeEnumerator<Player>());

                foreach (var player in players.Where(player => !NearPlayersList.Exists(nearPlayers => nearPlayers.LevelName == player.LevelFile)))
                    NearPlayersList.Add(new NearPlayers(player.LevelFile, null));
                
                for (var i = 0; i < NearPlayersList.Count; i++)
                {
                    var nearPlayers = NearPlayersList[i];

                    var playerList = new List<Player>();
                    foreach (var player in players.Where(player => nearPlayers.LevelName == player.LevelFile))
                        playerList.Add(player);

                    NearPlayersList[i] = new NearPlayers(nearPlayers.LevelName, playerList.ToArray());
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
            while (true)
            {
                for (int i = 0; i < NearPlayersList.Count; i++)
                {
                    var players = NearPlayersList[i];
                    if (players.Players != null)
                        for (var index = 0; index < players.Players.Length; index++)
                        {
                            var player = players.Players[index];
                            if (player.IsMovingNew)
                            {
                                //    if (players.Players[index].IsMoving)
                                //        players.Players[index].SendGameDataPlayers(players.Players);
                                for (var index1 = 0; index1 < players.Players.Length; index1++)
                                {
                                    var playerToSend = players.Players[index1];
                                    if (player != playerToSend)
                                        playerToSend.SendPacket(new GameDataPacket {DataItems = player.GenerateDataItems() }, player.ID);
                                }
                            }
                        }
                }



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
                    Logger.Log(LogType.Server, string.Format("The player {0} joined the game from IP {1}", playerToAdd.Name, playerToAdd.IP));
                    SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems(string.Format("Player {0} joined the game!", playerToAdd.Name)) });
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

                    Logger.Log(LogType.Server, string.Format("The player {0} disconnected, playtime was {1:hh\\:mm\\:ss}", playerToRemove.Name, DateTime.Now - playerToRemove.ConnectionTime));
                    SendToAllPlayers(new ChatMessagePacket { DataItems = new DataItems(string.Format("Player {0} disconnected!", playerToRemove.Name)) });
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

        public void ExecuteCommand(string message)
        {
            var command = message.ToLower();

            if (message.StartsWith("say "))
                SendGlobalChatMessageToAll(message.Remove(0, 4));

            else if (message.StartsWith("message "))
                SendServerMessageToAll(message.Remove(0, 8));

            else if (command.StartsWith("help server"))    // help from program
                ExecuteHelpCommand(message.Remove(0, 11));

            else if (command.StartsWith("help"))           // internal help from remote
                ExecuteHelpCommand(message.Remove(0, 4));

            else if (command.StartsWith("world "))
                ExecuteWorldCommand(command.Remove(0, 6));

            else
                InputWrapper.ConsoleWrite("Invalid command!");
        }

        private void ExecuteWorldCommand(string command)
        {
            if (command.StartsWith("enable") || command.StartsWith("enable custom"))
            {
                CustomWorldEnabled = true;
                InputWrapper.ConsoleWrite("Enabled Custom World!");
            }

            else if (command.StartsWith("disable") || command.StartsWith("disable custom"))
            {
                CustomWorldEnabled = false;
                InputWrapper.ConsoleWrite("Disabled Custom World!");
            }

            else if (command.StartsWith("set "))
            {
                command = command.Remove(0, 4);

                #region Weather
                if (command.StartsWith("weather "))
                {
                    command = command.Remove(0, 8);

                    Weather weather;
                    if (Enum.TryParse(command, true, out weather))
                    {
                        World.Weather = weather;
                        InputWrapper.ConsoleWrite(string.Format("Set Weather to {0}!", weather));
                    }
                    else
                        InputWrapper.ConsoleWrite("Weather not found!");
                }
                #endregion Weather

                #region Season
                else if (command.StartsWith("season "))
                {
                    command = command.Remove(0, 7);

                    Season season;
                    if (Enum.TryParse(command, true, out season))
                    {
                        World.Season = season;
                        InputWrapper.ConsoleWrite(string.Format("Set Season to {0}!", season));
                    }
                    else
                        InputWrapper.ConsoleWrite("Season not found!");
                }
                #endregion Season

                #region Time
                else if (command.StartsWith("time "))
                {
                    command = command.Remove(0, 5);

                    TimeSpan time;
                    if (TimeSpan.TryParseExact(command, "hh\\:mm\\:ss", null, out time))
                    {
                        World.CurrentTime = time;
                        World.UseRealTime = false;
                        InputWrapper.ConsoleWrite(string.Format("Set time to {0}!", time));
                        InputWrapper.ConsoleWrite("Disabled Real Time!");
                    }
                    else
                        InputWrapper.ConsoleWrite("Invalid time!");
                }
                #endregion Time

                #region DayCycle
                else if (command.StartsWith("daycycle "))
                {
                    command = command.Remove(0, 9);

                    World.DoDayCycle = command.StartsWith("true");
                    InputWrapper.ConsoleWrite(string.Format("Set Day Cycle to {0}!", World.DoDayCycle));
                }
                #endregion DayCycle

                #region Realtime
                else if (command.StartsWith("realtime "))
                {
                    command = command.Remove(0, 9);

                    World.UseRealTime = command.StartsWith("true");
                    World.DoDayCycle = true;
                    InputWrapper.ConsoleWrite(string.Format("Set Real Time to {0}!", World.UseRealTime));
                    InputWrapper.ConsoleWrite("Enabled Day Cycle!");
                }
                #endregion Realtime

                #region Location
                else if (command.StartsWith("location "))
                {
                    command = command.Remove(0, 9);

                    World.Location = command;
                    World.UseLocation = true;
                    InputWrapper.ConsoleWrite(string.Format("Set Location to {0}!", World.Location));
                    InputWrapper.ConsoleWrite("Enabled Location!");
                }
                #endregion Location

                else
                    InputWrapper.ConsoleWrite("Invalid command!");
            }

            else
                InputWrapper.ConsoleWrite("Invalid command!");
        }

        private static void ExecuteHelpCommand(string command)
        {

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


            if (Players != null)
                Players.Clear();

            if (PlayersJoining != null)
                PlayersJoining.Clear();

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
            public IClient Player;
            public IPacket Packet;
            public int OriginID;

            public PlayerPacket(IClient player, ref IPacket packet, int originID = -1)
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
