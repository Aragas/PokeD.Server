using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Server;
using PokeD.Core.Wrappers;

using PokeD.Server.Data;
using GameDataPacket = PokeD.Core.Packets.Shared.GameDataPacket;

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

        ConcurrentQueue<PlayerPacket> PacketsToPlayer { get; set; }
        ConcurrentQueue<OriginPacket> PacketsToAllPlayers { get; set; }


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

            Start();
        }

        public void Start()
        {
            ThreadWrapper.StartThread(ListenToClients,  true,   "ClientListnerThread");
            //ThreadWrapper.StartThread(ProcessClients,   true,   "ClientProcessorThread");
            ThreadWrapper.StartThread(World.Update,     true,   "WorldProcessorThread");
        }

        private void ListenToClients()
        {
            Listener = NetworkTCPServerWrapper.NewInstance(Port);
            Listener.Start();

            while (true)
                PlayersJoining.Add(new Player(Listener.AcceptNetworkTCPClient(), this));
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


        public void SendToAllPlayers(IPacket packet, int origin = -1)
        {
            PacketsToAllPlayers.Enqueue(new OriginPacket(origin, ref packet));
        }


        private int counter = 0;
        public void Update()
        {
            for (int i = 0; i < PlayersToAdd.Count; i++)
            {
                var player = PlayersToAdd[i];
                Players.Add(player);
                PlayersToAdd.Remove(player);

                SendToAllPlayers(new ChatMessagePacket
                {
                    DataItems = new DataItems(string.Format("Player {0} joined the game!", player.Name))
                });
            }

            for (int i = 0; i < PlayersToRemove.Count; i++)
            {
                var player = PlayersToRemove[i];
                Players.Remove(player);
                PlayersToRemove.Remove(player);
            }


            PlayersToUpdate.Clear();

            // Add actual players
            for (int i = 0; i < Players.Count; i++)
                PlayersToUpdate.Add(Players[i]);

            // Add joining players
            for (int i = 0; i < PlayersJoining.Count; i++)
                PlayersToUpdate.Add(PlayersJoining[i]);

            for (int i = 0; i < PlayersToUpdate.Count; i++)
                PlayersToUpdate[i].Update();


            PlayerPacket playerPacket;
            while (PacketsToPlayer.TryDequeue(out playerPacket))
                playerPacket.Player.SendPacket(playerPacket.Packet);

            OriginPacket originPacket;
            while (PacketsToAllPlayers.TryDequeue(out originPacket))
                foreach (var player in Players)
                    player.SendPacketCustom(originPacket.Packet, originPacket.Origin);
        }

        public int GenerateID()
        {
            return Players.Count + 3;
        }


        public void Dispose()
        {
            
        }




        public void ExecuteClientCommand(string message)
        {

        }

        public bool PlayerIsMuted(Player player)
        {
            return false;
        }

        public bool PlayerIsMuted(int origin)
        {
            return false;
        }

        public bool PlayerOnline(string name)
        {
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].Name == name)
                    return true;

            return false;
        }
        
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

        public string GetPlayerName(int id)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.ID == id)
                    return player.Name;
            }

            return null;
        }

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

        public void SendToPlayer(int destinationID, IPacket packet, int origin = -1)
        {
            var player = GetPlayer(destinationID);
            
            if(player != null)
                player.SendPacketCustom(packet, origin);
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
    }
}
