using System.Collections.Concurrent;
using System.Collections.Generic;

using Newtonsoft.Json;

using PokeD.Core.Data.Structs;
using PokeD.Core.Packets.Server;

using PokeD.Server.Clients;
using PokeD.Server.Clients.SCON;
using PokeD.Server.Extensions;

namespace PokeD.Server
{
    public partial class Server
    {
        [JsonIgnore]
        public int PlayersCount => Players.Count;

        int FreePlayerID { get; set; } = 10;


        ClientList Players { get; } = new ClientList();
        List<IClient> PlayersJoining { get; } = new List<IClient>();
        List<IClient> PlayersToAdd { get; } = new List<IClient>();
        List<IClient> PlayersToRemove { get; } = new List<IClient>();
        List<IClient> SCONClients { get; } = new List<IClient>();

        ConcurrentDictionary<string, IClient[]> NearPlayers { get; } = new ConcurrentDictionary<string, IClient[]>();

        ConcurrentQueue<PlayerPacketP3DOrigin> PacketsToPlayer { get; set; } = new ConcurrentQueue<PlayerPacketP3DOrigin>();
        ConcurrentQueue<PacketP3DOrigin> PacketsToAllPlayers { get; set; } = new ConcurrentQueue<PacketP3DOrigin>();

        [JsonProperty("MutedPlayers")]
        Dictionary<int, List<int>> MutedPlayers { get; } = new Dictionary<int, List<int>>();



        private int GenerateClientID()
        {
            return FreePlayerID++;
        }


        public void AddPlayer(IClient player)
        {
            if (player is SCONClient)
            {
                SCONClients.Add(player);
                PlayersJoining.Remove(player);
                return;
            }

            player.LoadClientSettings();

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
            player.SaveClientSettings();

            PlayersToRemove.Add(player);
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
