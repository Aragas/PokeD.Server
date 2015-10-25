using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

using PokeD.Core.Data.Structs;
using PokeD.Core.Packets.Server;

using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server
    {
        [JsonIgnore]
        public int PlayersCount => Players.Count;

        ClientList Players { get; } = new ClientList();
        List<IClient> PlayersJoining { get; } = new List<IClient>();
        List<IClient> PlayersToAdd { get; } = new List<IClient>();
        List<IClient> PlayersToRemove { get; } = new List<IClient>();
        
        ConcurrentDictionary<string, IClient[]> NearPlayers { get; } = new ConcurrentDictionary<string, IClient[]>();

        ConcurrentQueue<PlayerPacketP3DOrigin> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketP3DOrigin>();
        ConcurrentQueue<PacketP3DOrigin> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<PacketP3DOrigin>();

        [JsonProperty("MutedPlayers")]
        Dictionary<int, List<int>> MutedPlayers { get; } = new Dictionary<int, List<int>>();

        List<IClient> AllClients { get { var list = new List<IClient>(Players.GetEnumerator()); list.AddRange(NPCs); return list; } }


        private bool IsGameJoltIDUsed(IClient client)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (player.IsGameJoltPlayer && client.GameJoltID == player.GameJoltID)
                    return true;
            }

            return false;
        }


        public void AddPlayer(IClient player)
        {
            if (IsGameJoltIDUsed(player))
            {
                RemovePlayer(player, "You are already on server!");
                return;
            }

            LoadDBPlayer(player);

            SendToClient(player, new IDPacket { PlayerID = player.ID }, -1);
            SendToClient(player, new WorldDataPacket { DataItems = World.GenerateDataItems() }, -1);

            // Send to player his ID
            SendToClient(player, new CreatePlayerPacket { PlayerID = player.ID }, -1);
            // Send to player all Players ID
            var clients = AllClients;
            for (var i = 0; i < clients.Count; i++)
            {
                SendToClient(player, new CreatePlayerPacket { PlayerID = clients[i].ID }, -1);
                SendToClient(player, clients[i].GetDataPacket(), clients[i].ID);
            }
            // Send to Players player ID
            SendToAllClients(new CreatePlayerPacket { PlayerID = player.ID }, -1);
            SendToAllClients(player.GetDataPacket(), player.ID);


            PlayersToAdd.Add(player);
            PlayersJoining.Remove(player);
        }
        public void RemovePlayer(IClient player, string reason = "")
        {
            UpdateDBPlayer(player);

            if(!string.IsNullOrEmpty(reason))
                player.SendPacket(new KickedPacket { Reason = reason }, -1);

            PlayersToRemove.Add(player);
        }

        private void LoadDBPlayer(IClient player)
        {
            if (player.IsGameJoltPlayer)
            {
                var data = Database.Find<Player>(p => p.GameJoltID == player.GameJoltID);

                if (data != null)
                    player.LoadFromDB(data);
                else
                {
                    Database.Insert(new Player(player, PlayerType.Player));
                    player.LoadFromDB(Database.Find<Player>(p => p.GameJoltID == player.GameJoltID));
                }
            }
            else
            {
                var data = Database.Find<Player>(p => p.Name == player.Name);

                if (data != null)
                    player.LoadFromDB(data);
                else
                {
                    Database.Insert(new Player(player, PlayerType.Player));
                    player.LoadFromDB(Database.Find<Player>(p => p.Name == player.Name));
                }
            }
        }

        Stopwatch UpdateDBWatch { get; } = Stopwatch.StartNew();
        public void UpdateDBPlayer(IClient player)
        {
            if(player.ID == 0)
                return;

            if (UpdateDBWatch.ElapsedMilliseconds < 2000)
                return;
            
            Database.Update(new Player(player, PlayerType.Player));

            UpdateDBWatch.Reset();
            UpdateDBWatch.Start();
        }


        /// <summary>
        /// Get IClient by ID.
        /// </summary>
        /// <param name="id">IClient ID.</param>
        /// <returns>Returns null if IClient is not found.</returns>
        public IClient GetClient(int id)
        {
            var clients = AllClients;
            for (var i = 0; i < clients.Count; i++)
            {
                var player = clients[i];
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
        public PlayerInfo[] GetAllClientsInfo()
        {
            return Players.GetAllClientsInfo();
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
    }
}
