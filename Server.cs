using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

using PokeD.Core.Data;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Server;

using PokeD.Server.Clients;
using PokeD.Server.Clients.NPC;
using PokeD.Server.Clients.P3D;
using PokeD.Server.Clients.Protobuf;
using PokeD.Server.Clients.SCON;
using PokeD.Server.Data;
using PokeD.Server.Extensions;

namespace PokeD.Server
{
    public partial class Server : IUpdatable, IDisposable
    {
        const string FileName = "Server.json";


        #region Settings

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

        [JsonProperty("EncryptionEnabled")]
        public bool EncryptionEnabled { get; set; } = true;

        [JsonProperty("SCON_Enabled")]
        public bool SCON_Enabled { get; set; } = true;

        [JsonProperty("SCON_Password"), JsonConverter(typeof(PasswordHandler))]
        public PasswordStorage SCON_Password { get; private set; } = new PasswordStorage();

        [JsonProperty("World")]
        World World { get; set; } = new World();

        [JsonProperty("CustomWorldEnabled")]
        public bool CustomWorldEnabled { get; private set; } = true;

        [JsonProperty("MoveCorrectionEnabled")]
        bool MoveCorrectionEnabled { get; set; } = true;

        #endregion Settings


        INetworkTCPServer P3DListener { get; set; }
        INetworkTCPServer ProtobufListener { get; set; }
        INetworkTCPServer SCONListener { get; set; }

        
        int ListenToConnectionsThread { get; set; }
        int PlayerWatcherThread { get; set; }
        int PlayerCorrectionThread { get; set; }


        [JsonIgnore]
        public bool IsDisposing { get; private set; }

        [JsonIgnore]
        public AsymmetricCipherKeyPair RSAKeyPair { get; private set; }
        const int RsaKeySize = 1024;



        public Server()
        {

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

            if (EncryptionEnabled)
            {
                Logger.Log(LogType.Info, "Generating RSA key pair.");
                RSAKeyPair = GenerateKeyPair();
            }

            StartNPCs();

            Logger.Log(LogType.Info, $"Starting {ServerName}.");


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

            Logger.Log(LogType.Info, $"Stopping {ServerName}.");


            if (ThreadWrapper.IsRunning(ListenToConnectionsThread))
                ThreadWrapper.AbortThread(ListenToConnectionsThread);

            if (ThreadWrapper.IsRunning(PlayerWatcherThread))
                ThreadWrapper.AbortThread(PlayerWatcherThread);

            if (ThreadWrapper.IsRunning(PlayerCorrectionThread))
                ThreadWrapper.AbortThread(PlayerCorrectionThread);

            Dispose();

            Logger.Log(LogType.Info, $"Stopped {ServerName}.");

            return status;
        }

        private bool StartNPCs()
        {
            Logger.Log(LogType.Info, "Loading NPC's.");
            NPCs = NPCLoader.LoadNPCs(this);

            foreach (var npc in NPCs)
                npc.ID = GenerateClientID();

            return true;
        }
        public bool ReloadNPCs()
        {
            foreach (var npc in NPCs)
                PlayersToRemove.Add(npc);

            Logger.Log(LogType.Info, "Reloading NPC's.");
            NPCs = NPCLoader.LoadNPCs(this);

            foreach (var npc in NPCs)
                npc.ID = GenerateClientID();

            return true;
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

            if (SCON_Enabled && SCONPort != 0)
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
                var players = new List<IClient>(Players.GetConcreteTypeEnumerator<P3DPlayer, ProtobufPlayer>());

                foreach (var player in players.Where(player => player.LevelFile != null && !NearPlayers.ContainsKey(player.LevelFile)))
                    NearPlayers.TryAdd(player.LevelFile, null);

                foreach (var level in NearPlayers.Keys)
                {
                    var playerList = new List<IClient>();
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


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            UpdateNPC();


            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Players.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    Logger.Log(LogType.Server, $"The player {playerToAdd.Name} joined the game from IP {playerToAdd.IP}.");
                    SendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToAdd.Name} joined the game!" });
                }
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Players.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                SCONClients.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

                if (playerToRemove.ID != 0)
                {
                    SendToAllClients(new DestroyPlayerPacket { PlayerID = playerToRemove.ID });

                    Logger.Log(LogType.Server, $"The player {playerToRemove.Name} disconnected, playtime was {DateTime.Now - playerToRemove.ConnectionTime:hh\\:mm\\:ss}.");
                    SendToAllClients(new ChatMessageGlobalPacket { Message = $"Player {playerToRemove.Name} disconnected!" });
                }

                playerToRemove.Dispose();
            }

            #endregion Player Filtration

            
            #region Player Updating

            // Update actual players
            for (var i = 0; i < Players.Count; i++)
                Players[i]?.Update();
            
            // Update joining players
            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i]?.Update();

            // Update SCON clients
            for (var i = 0; i < SCONClients.Count; i++)
                SCONClients[i]?.Update();

            #endregion Player Updating


            #region Packet Sending

            PlayerPacketP3DOrigin packetToPlayer;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out packetToPlayer))
                packetToPlayer.Player.SendPacket(packetToPlayer.Packet, packetToPlayer.OriginID);
            
            PacketP3DOrigin packetToAllPlayers;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out packetToAllPlayers))
                for (var i = 0; i < Players.Count; i++)
                    Players[i].SendPacket(packetToAllPlayers.Packet, packetToAllPlayers.OriginID);

            #endregion Packet Sending



            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if(player == null) continue;

                if (!player.UseCustomWorld)
                    SendToClient(player, new WorldDataPacket { DataItems = World.GenerateDataItems() }, -1);
            }

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }

        public void UpdateNPC()
        {
            for (var i = 0; i < NPCs.Count; i++)
                NPCs[i]?.Update();
        }


        public void SendToClient(int destinationID, P3DPacket packet, int originID)
        {
            SendToClient(GetClient(destinationID), packet, originID);
        }
        public void SendToClient(IClient player, P3DPacket packet, int originID)
        {
            if (player != null)
                PacketsToPlayer.Enqueue(new PlayerPacketP3DOrigin(player, ref packet, originID));
        }
        public void SendToAllClients(P3DPacket packet, int originID = -1)
        {
            if (originID != -1 && (packet is ChatMessageGlobalPacket || packet is ChatMessagePrivatePacket))
                if (MutedPlayers.ContainsKey(originID) && MutedPlayers[originID].Count > 0)
                {
                    for (var i = 0; i < Players.Count; i++)
                    {
                        var player = Players[i];
                        if (!MutedPlayers[originID].Contains(player.ID))
                            PacketsToPlayer.Enqueue(new PlayerPacketP3DOrigin(player, ref packet, originID));
                    }

                    return;
                }

            PacketsToAllPlayers.Enqueue(new PacketP3DOrigin(ref packet, originID));
        }


        public void SendPrivateChatMessageToClient(int destinationID, string message, int originID)
        {
            SendPrivateChatMessageToClient(GetClient(destinationID), message, originID);
        }
        public void SendPrivateChatMessageToClient(IClient player, string message, int originID)
        {
            if (player != null)
                PacketsToPlayer.Enqueue(new PlayerPacketP3DOrigin(player, new ChatMessagePrivatePacket { DataItems = new DataItems(message) }, originID));
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
                    player.SendPacket(new ChatMessageGlobalPacket { Message = message }, -1);
            }
        }

        
        public void Dispose()
        {
            if(IsDisposing)
                return;

            IsDisposing = true;


            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();

            for (var i = 0; i < Players.Count; i++)
            {
                Players[i].SendPacket(new ServerClosePacket { Reason = "Closing server!" }, -1);
                Players[i].Dispose();
            }
            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new ServerClosePacket { Reason = "Closing server!" }, -1);
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
