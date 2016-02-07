using System.Collections.Concurrent;
using System.Collections.Generic;

using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Packets;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;

using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;

namespace PokeD.Server
{
    public class ModuleSCON : IServerModule
    {
        const string FileName = "ModuleSCON.json";

        #region Settings

        [JsonProperty("Enabled")]
        public bool Enabled { get; private set; } = false;

        [JsonProperty("Port")]
        public ushort Port { get; private set; } = 15126;

        [JsonProperty("SCON_Password"), JsonConverter(typeof(PasswordHandler))]
        public PasswordStorage SCON_Password { get; private set; } = new PasswordStorage();

        [JsonProperty("EncryptionEnabled")]
        public bool EncryptionEnabled { get; private set; } = true;
        
        #endregion Settings

        [JsonIgnore]
        public Server Server { get; }
        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        [JsonIgnore]
        public ClientList Clients { get; } = new ClientList();
        [JsonIgnore]
        public bool ClientsVisible { get; } = false;
        List<IClient> PlayersJoining { get; } = new List<IClient>();
        List<IClient> PlayersToAdd { get; } = new List<IClient>();
        List<IClient> PlayersToRemove { get; } = new List<IClient>();

        ConcurrentQueue<PlayerPacketSCON> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketSCON>();
        ConcurrentQueue<SCONPacket> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<SCONPacket>();


        public ModuleSCON(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemWrapper.LoadSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load SCON settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"SCON not enabled!");
                return false;
            }

            Logger.Log(LogType.Info, $"Starting SCON.");

            return true;
        }
        public void Stop()
        {
            var status = FileSystemWrapper.SaveSettings(FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save SCON settings!");
            
            Logger.Log(LogType.Info, $"Stopping SCON.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped SCON.");
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
                    PlayersJoining.Add(new SCONClient(Listener.AcceptTCPClient(), this));
        }


        public void AddClient(IClient client)
        {
            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);
        }
        public void RemoveClient(IClient client, string reason = "")
        {
            if (!string.IsNullOrEmpty(reason))
                client.SendPacket(new AuthorizationDisconnectPacket { Reason = reason });

            PlayersToRemove.Add(client);
        }


        public void Update()
        {
            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Clients.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

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

            PlayerPacketSCON packetToPlayer;
            while (!IsDisposing && PacketsToPlayer.TryDequeue(out packetToPlayer))
                packetToPlayer.Player.SendPacket(packetToPlayer.Packet);

            SCONPacket packetToAllPlayers;
            while (!IsDisposing && PacketsToAllPlayers.TryDequeue(out packetToAllPlayers))
                for (var i = 0; i < Clients.Count; i++)
                    Clients[i].SendPacket(packetToAllPlayers);

            #endregion Packet Sending
        }


        public void OtherConnected(IClient client) { }
        public void OtherDisconnected(IClient client) { }

        public void SendServerMessage(IClient sender, string message)
        {
            if (sender is SCONClient)
                return;
            else
                SCONClientSendToAllClients(new ChatMessagePacket { Player = sender.Name, Message = message });
        }
        public void SendPrivateMessage(IClient sender, IClient destClient, string message) { }
        public void SendGlobalMessage(IClient sender, string message)
        {
            if (sender is SCONClient)
                return;
            else
                SCONClientSendToAllClients(new ChatMessagePacket { Player = sender.Name, Message = message });
        }

        public void SendTradeRequest(IClient sender, Monster monster, IClient destClient) { }
        public void SendTradeConfirm(IClient sender, IClient destClient) { }
        public void SendTradeCancel(IClient sender, IClient destClient) { }

        public void SendPosition(IClient sender) { }

        public void ExecuteCommand(string command) { }


        public void SCONClientSendToAllClients(SCONPacket packet)
        {
            PacketsToAllPlayers.Enqueue(packet);
        }


        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();
            PlayersJoining.Clear();

            for (var i = 0; i < Clients.Count; i++)
            {
                Clients[i].SendPacket(new AuthorizationDisconnectPacket() { Reason = "Closing server!" }, -1);
                Clients[i].Dispose();
            }
            Clients.Clear();

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new AuthorizationDisconnectPacket() { Reason = "Closing server!" }, -1);
                PlayersToAdd[i].Dispose();
            }
            PlayersToAdd.Clear();

            // Do not dispose PlayersToRemove!
            PlayersToRemove.Clear();


            PacketsToPlayer = null;
            PacketsToAllPlayers = null;
        }


        private class PlayerPacketSCON
        {
            public readonly IClient Player;
            public readonly SCONPacket Packet;

            public PlayerPacketSCON(IClient player, ref SCONPacket packet)
            {
                Player = player;
                Packet = packet;
            }
            public PlayerPacketSCON(IClient player, SCONPacket packet)
            {
                Player = player;
                Packet = packet;
            }
        }
    }
}
