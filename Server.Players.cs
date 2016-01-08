using System.Collections.Generic;
using System.Diagnostics;

using PokeD.Core.Data.SCON;

using PokeD.Server.Clients;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server
    {
        public void LoadDBPlayer(IClient player)
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



        public List<IClient> AllClients()
        {
            var list = new List<IClient>();
            foreach (var module in Modules)
                list.AddRange(module.Clients.GetEnumerator());
            //list.AddRange(NPCs);

            return list;
        }

        ///// <summary>
        ///// Get all connected IClient Names.
        ///// </summary>
        ///// <returns>Returns null if there are no IClient connected.</returns>
        public PlayerInfo[] GetAllClientsInfo()
        {
            var list = new List<PlayerInfo>();
            foreach (var module in Modules)
                list.AddRange(module.Clients.GetAllClientsInfo());

            return list.ToArray();
        }



        /// <summary>
        /// Get IClient by ID.
        /// </summary>
        /// <param name="id">IClient ID.</param>
        /// <returns>Returns null if IClient is not found.</returns>
        public IClient GetClient(int id)
        {
            var clients = AllClients();
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
            var clients = AllClients();
            for (var i = 0; i < clients.Count; i++)
            {
                var player = clients[i];
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
        public int GetClientID(string name) => GetClient(name)?.ID ?? -1;

        /// <summary>
        /// Get IClient ID by Name.
        /// </summary>
        /// <param name="id">IClient ID.</param>
        /// <returns>Returns -1 if IClient is not found.</returns>
        public string GetClientName(int id) => GetClient(id)?.Name ?? string.Empty;
    }
}
