﻿using System.Collections.Concurrent;
using System.Collections.Generic;

using Aragas.Network.Data;

using PCLExt.Config;
using PCLExt.Config.Extensions;
using PCLExt.Network;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;

namespace PokeD.Server
{
    public class ModuleSCON : IServerModule
    {
        const string FileName = "ModuleSCON";

        #region Settings

        public bool Enabled { get; private set; } = false;

        public ushort Port { get; private set; } = 15126;

        public PasswordStorage SCON_Password { get; private set; } = new PasswordStorage();

        public bool EncryptionEnabled { get; private set; } = true;
        
        #endregion Settings

        [ConfigIgnore]
        public Server Server { get; }
        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }

        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public bool ClientsVisible { get; } = false;
        List<Client> PlayersJoining { get; } = new List<Client>();
        List<Client> PlayersToAdd { get; } = new List<Client>();
        List<Client> PlayersToRemove { get; } = new List<Client>();

        ConcurrentQueue<PlayerPacketSCON> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketSCON>();
        ConcurrentQueue<SCONPacket> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<SCONPacket>();


        public ModuleSCON(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(Server.ConfigType, FileName, this);
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
            var status = FileSystemExtensions.SaveConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save SCON settings!");
            
            Logger.Log(LogType.Info, $"Stopping SCON.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped SCON.");
        }
        
        
        public void StartListen()
        {
            Listener = SocketServer.CreateTCP(Port);
            Listener.Start();
        }
        public void CheckListener()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    PlayersJoining.Add(new SCONClient(Listener.AcceptTCPClient(), this));
        }


        public void AddClient(Client client)
        {
            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);
        }
        public void RemoveClient(Client client, string reason = "")
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


        public void ClientConnected(Client client) { }
        public void ClientDisconnected(Client client) { }

        public void SendServerMessage(Client sender, string message, bool fromServer = false)
        {
            if (sender is SCONClient)
                return;
            else
                SCONClientSendToAllClients(new ChatMessagePacket { Player = sender.Name, Message = message });
        }
        public void SendPrivateMessage(Client sender, Client destClient, string message, bool fromServer = false) { }
        public void SendGlobalMessage(Client sender, string message, bool fromServer = false)
        {
            if (sender is SCONClient)
                return;
            else
                SCONClientSendToAllClients(new ChatMessagePacket { Player = sender.Name, Message = message });
        }

        public void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false) { }
        public void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false) { }
        public void SendTradeCancel(Client sender, Client destClient, bool fromServer = false) { }

        public void SendPosition(Client sender, bool fromServer = false) { }

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
                Clients[i].SendPacket(new AuthorizationDisconnectPacket() { Reason = "Closing server!" });
                Clients[i].Dispose();
            }
            Clients.Clear();

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].SendPacket(new AuthorizationDisconnectPacket() { Reason = "Closing server!" });
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
            public readonly Client Player;
            public readonly SCONPacket Packet;

            public PlayerPacketSCON(Client player, ref SCONPacket packet)
            {
                Player = player;
                Packet = packet;
            }
            public PlayerPacketSCON(Client player, SCONPacket packet)
            {
                Player = player;
                Packet = packet;
            }
        }
    }
}
