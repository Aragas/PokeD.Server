using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Newtonsoft.Json;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Server;
using PokeD.Core.Wrappers;

using PokeD.Server.Clients;
using PokeD.Server.Clients.P3D;
using PokeD.Server.Clients.Protobuf;
using PokeD.Server.Clients.SCON;
using PokeD.Server.Data;
using PokeD.Server.Extensions;

namespace PokeD.Server
{
    public partial class Server : IUpdatable, IDisposable
    {
        public const string FileName = "Server.json";

        [JsonIgnore]
        public float P3DProtocolVersion => 0.5f;


        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 15124;

        [JsonProperty("ProtobufPort")]
        public ushort ProtobufPort { get; private set; } = 15125;

        [JsonProperty("SCONPort")]
        public ushort SCONPort { get; private set; } = 15126;


        [JsonProperty("ServerName", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerName { get; private set; } = "Put name here";

        [JsonProperty("ServerMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerMessage { get; private set; } = "Put description here";

        [JsonProperty("MaxPlayers")]
        public int MaxPlayers { get; private set; } = 1000;

        [JsonProperty("SCONEnabled")]
        public bool SCONEnabled { get; set; } = true;

        [JsonProperty("EncryptionEnabled")]
        public bool EncryptionEnabled { get; set; } = true;

        [JsonProperty("MoveCorrectionEnabled")]
        bool MoveCorrectionEnabled { get; set; } = true;

        [JsonProperty("World")]
        World World { get; set; } = new World();

        [JsonProperty("CustomWorldEnabled")]
        public bool CustomWorldEnabled { get; private set; } = true;

        [JsonProperty("SCON_Password", NullValueHandling = NullValueHandling.Ignore)]
        public string SCON_Password { get; private set; } = "PUT_PASSWORD_HERE";


        INetworkTCPServer P3DListener { get; set; }
        INetworkTCPServer ProtobufListener { get; set; }
        INetworkTCPServer SCONListener { get; set; }


        #region Player Stuff

        [JsonIgnore]
        public int PlayersCount => Players.Count;

        ClientList Players { get; } = new ClientList();
        List<IClient> PlayersJoining { get; } = new List<IClient>();
        List<IClient> PlayersToAdd { get; } = new List<IClient>();
        List<IClient> PlayersToRemove { get; } = new List<IClient>();
        List<IClient> SCONClients { get; } = new List<IClient>();

        ConcurrentDictionary<string, P3DPlayer[]> NearPlayers { get; } = new ConcurrentDictionary<string, P3DPlayer[]>();

        ConcurrentQueue<PlayerP3DPacket> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerP3DPacket>();
        ConcurrentQueue<OriginP3DPacket> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<OriginP3DPacket>();

        [JsonProperty("MutedPlayers")]
        Dictionary<int, List<int>> MutedPlayers { get; } = new Dictionary<int, List<int>>();

        int FreePlayerID { get; set; } = 10;

        #endregion Player Stuff


        int ListenToConnectionsThread { get; set; }
        int PlayerWatcherThread { get; set; }
        int PlayerCorrectionThread { get; set; }

        [JsonIgnore]
        public bool IsDisposing { get; private set; }

        [JsonIgnore]
        public AsymmetricCipherKeyPair RSAKeyPair { get; private set; }
        const int RsaKeySize = 1024;

        public Server() { }


        public Server(ushort port = 15124, ushort protobufPort = 15125, ushort sconPort = 15126) : this()
        {
            Port = port;
            ProtobufPort = protobufPort;
            SCONPort = sconPort;
        }

        private static AsymmetricCipherKeyPair GenerateKeyPair()
        {
            var secureRandom = new SecureRandom(new DigestRandomGenerator(new Sha512Digest()));
            var keyGenerationParameters = new KeyGenerationParameters(secureRandom, RsaKeySize);

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            return keyPairGenerator.GenerateKeyPair();
        }


        public bool Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if(!status)
                Logger.Log(LogType.Warning, "Failed to load Server settings!");

            if(EncryptionEnabled)
                RSAKeyPair = GenerateKeyPair();

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
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save Server settings!");

            Logger.Log(LogType.Info, $"Stopping {ServerName}");


            if (ThreadWrapper.IsRunning(ListenToConnectionsThread))
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
            P3DListener = NetworkTCPServerWrapper.NewInstance(Port);
            P3DListener.Start();

            if (ProtobufPort != 0)
            {
                ProtobufListener = NetworkTCPServerWrapper.NewInstance(ProtobufPort);
                ProtobufListener.Start();
            }

            if (SCONEnabled && SCONPort != 0)
            {
                SCONListener = NetworkTCPServerWrapper.NewInstance(SCONPort);
                SCONListener.Start();
            }

            var watch = Stopwatch.StartNew();
            while (!IsDisposing)
            {
                if (P3DListener.AvailableClients)
                    PlayersJoining.Add(new P3DPlayer(P3DListener.AcceptNetworkTCPClient(), this));

                if (ProtobufListener != null && ProtobufListener.AvailableClients)
                    if (ProtobufListener.AvailableClients)
                        PlayersJoining.Add(new ProtobufPlayer(ProtobufListener.AcceptNetworkTCPClient(), this));

                if (SCONListener != null && SCONListener.AvailableClients)
                    if (SCONListener.AvailableClients)
                        SCONClients.Add(new SCONClient(SCONListener.AcceptNetworkTCPClient(), this));



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
                var players = new List<P3DPlayer>(Players.GetConcreteTypeEnumerator<P3DPlayer>());

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
                    foreach (var player in nearPlayers.Value.Where(player => player.IsMoving))
                        foreach (var playerToSend in nearPlayers.Value.Where(playerToSend => player != playerToSend))
                            playerToSend.SendPacket(player.GetDataPacket(), player.ID);



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



        public void AddPlayer(IClient player)
        {
            FileSystemWrapperExtensions.LoadClientSettings(player);

            player.ID = GenerateClientID();
            SendToClient(player, new IDPacket { PlayerID = player.ID }, -1);
            SendToClient(player, new WorldDataPacket { DataItems = World.GenerateDataItems() }, -1);

            // Send to player his ID
            SendToClient(player, new CreatePlayerPacket { PlayerID = player.ID }, -1);
            // Send to player all Players ID
            for (var i = 0; i < Players.Count; i++)
            {
                SendToClient(player, new CreatePlayerPacket { PlayerID = Players[i].ID }, -1);
                SendToClient(player, Players[i].GetDataPacket(), Players[i].ID);
            }
            // Send to Players player ID
            SendToAllClients(new CreatePlayerPacket { PlayerID = player.ID }, -1);
            SendToAllClients(player.GetDataPacket(), player.ID);


            PlayersToAdd.Add(player);
            PlayersJoining.Remove(player);
        }
        public void RemovePlayer(IClient player)
        {
            FileSystemWrapperExtensions.SaveClientSettings(player);

            PlayersToRemove.Add(player);
        }


        public void SendToClient(int destinationID, P3DPacket packet, int originID)
        {
            SendToClient(GetClient(destinationID), packet, originID);
        }
        public void SendToClient(IClient player, P3DPacket packet, int originID)
        {
            if (player != null)
                PacketsToPlayer.Enqueue(new PlayerP3DPacket(player, ref packet, originID));
        }
        public void SendToAllClients(P3DPacket packet, int originID = -1)
        {
            if (originID != -1 && (packet is ChatMessagePacket || packet is ChatMessagePrivatePacket))
                if (MutedPlayers.ContainsKey(originID) && MutedPlayers[originID].Count > 0)
                {
                    for (var i = 0; i < Players.Count; i++)
                    {
                        var player = Players[i];
                        if (!MutedPlayers[originID].Contains(player.ID))
                            PacketsToPlayer.Enqueue(new PlayerP3DPacket(player , ref packet, originID));
                    }

                    return;
                }

            PacketsToAllPlayers.Enqueue(new OriginP3DPacket(ref packet, originID));
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            
            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Players.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    Logger.Log(LogType.Server, $"The player {playerToAdd.Name} joined the game from IP {playerToAdd.IP}");
                    SendToAllClients(new ChatMessagePacket { DataItems = new DataItems($"Player {playerToAdd.Name} joined the game!") });
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
                    SendToAllClients(new DestroyPlayerPacket { DataItems = new DataItems(playerToRemove.ID.ToString()) });

                    Logger.Log(LogType.Server, $"The player {playerToRemove.Name} disconnected, playtime was {DateTime.Now - playerToRemove.ConnectionTime:hh\\:mm\\:ss}");
                    SendToAllClients(new ChatMessagePacket { DataItems = new DataItems($"Player {playerToRemove.Name} disconnected!") });
                }
            }

            #endregion Player Filtration



            #region Player Updating

            // Update actual players
            for (var i = 0; i < Players.Count; i++)
                Players[i].Update();

            // Update joining players
            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Update();

            // Update SCON clients
            for (var i = 0; i < SCONClients.Count; i++)
                SCONClients[i].Update();
            
            #endregion Player Updating



            #region Packet Sending

            PlayerP3DPacket playerP3DPacket;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out playerP3DPacket))
                playerP3DPacket.Player.SendPacket(playerP3DPacket.Packet, playerP3DPacket.OriginID);

            OriginP3DPacket originP3DPacket;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out originP3DPacket))
                for (var i = 0; i < Players.Count; i++)
                    Players[i].SendPacket(originP3DPacket.Packet, originP3DPacket.OriginID);

            #endregion Packet Sending



            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (!player.UseCustomWorld)
                    SendToClient(player, new WorldDataPacket { DataItems = World.GenerateDataItems() }, -1);
            }

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        private int GenerateClientID()
        {
            return FreePlayerID++;
        }



        public MuteStatus MutePlayer(int id, string muteName)
        {
            if (!MutedPlayers.ContainsKey(id))
                MutedPlayers.Add(id, new List<int>());

            var muteID = GetClientID(muteName);
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

            var muteID = GetClientID(muteName);
            if (id == muteID)
                return MuteStatus.MutedYourself;

            if (muteID != -1)
            {
                MutedPlayers[id].Remove(muteID);
                return MuteStatus.Completed;
            }

            return MuteStatus.PlayerNotFound;
        }


        public void SendServerMessageToAllClients(string message)
        {
            for (var i = 0; i < Players.Count; i++)
                Players[i].SendPacket(new ServerMessagePacket { Message = message }, -1);
        }
        public void SendGlobalChatMessageToAllClients(string message)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if(player.ChatReceiving)
                    player.SendPacket(new ChatMessagePacket { Message = message }, -1);
            }
        }


        /// <summary>
        /// Get IClient by ID.
        /// </summary>
        /// <param name="id">IClient ID.</param>
        /// <returns>Returns null if IClient is not found.</returns>
        public IClient GetClient(int id)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.ID == id)
                    return player;
            }

            return null;
        }

        /// <summary>
        /// Get IClient by name.
        /// </summary>
        /// <param name="name">IClient Name.</param>
        /// <returns>Returns null if IClient is not found.</returns>
        public IClient GetClient(string name)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.Name == name)
                    return player;
            }

            return null;
        }

        /// <summary>
        /// Get IClient Name by ID.
        /// </summary>
        /// <param name="name">IClient Name.</param>
        /// <returns>Returns String.Empty if IClient is not found.</returns>
        public int GetClientID(string name)
        {
            return GetClient(name)?.ID ?? -1;
        }

        /// <summary>
        /// Get IClient ID by Name.
        /// </summary>
        /// <param name="id">IClient ID.</param>
        /// <returns>Returns -1 if IClient is not found.</returns>
        public string GetClientName(int id)
        {
            return GetClient(id)?.Name ?? string.Empty;
        }

        /// <summary>
        /// Get all connected IClient Names.
        /// </summary>
        /// <returns>Returns null if there are no IClient connected.</returns>
        public string[] GetClientNames()
        {
            var list = new List<string>();

            for (var i = 0; i < Players.Count; i++)
                list.Add(Players[i].Name);

            return list.ToArray();
        }


        public void Dispose()
        {
            IsDisposing = true;

            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();

            for (var i = 0; i < Players.Count; i++)
            {
                Players[i].SendPacket(new ServerClosePacket { Reason = "Closing server" }, -1);
                Players[i].Dispose();
            }
            for (var i = 0; i < PlayersToAdd.Count; i++)
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
