using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.SCON;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public partial class Server
    {
        /// <summary>
        /// Get all connected IClient.
        /// </summary>
        /// <returns>Returns null if there are no IClient connected.</returns>
        public IEnumerable<IClient> AllClients() => Modules.Where(module => module.ClientsVisible).SelectMany(module => module.Clients);

        /// <summary>
        /// Get all connected IClient Names.
        /// </summary>
        /// <returns>Returns null if there are no IClient connected.</returns>
        public IEnumerable<PlayerInfo> GetAllClientsInfo() => Modules.Where(module => module.ClientsVisible).SelectMany(module => module.Clients.GetAllClientsInfo());


        /// <summary>
        /// Get IClient by ID.
        /// </summary>
        /// <param name="id">IClient ID.</param>
        /// <returns>Returns null if IClient is not found.</returns>
        public IClient GetClient(int id) => AllClients().FirstOrDefault(client => client.ID == id);

        /// <summary>
        /// Get IClient by name.
        /// </summary>
        /// <param name="name">IClient Name.</param>
        /// <returns>Returns null if IClient is not found.</returns>
        public IClient GetClient(string name) => AllClients().FirstOrDefault(client => client.Name == name);

        /// <summary>
        /// Get IClient Name by ID.
        /// </summary>
        /// <param name="name">IClient Name.</param>
        /// <returns>Returns -1 if IClient is not found.</returns>
        public int GetClientID(string name) => GetClient(name)?.ID ?? -1;

        /// <summary>
        /// Get IClient ID by Name.
        /// </summary>
        /// <param name="id">IClient ID.</param>
        /// <returns>Returns <see cref="string.Empty"/> if IClient is not found.</returns>
        public string GetClientName(int id) => GetClient(id)?.Name ?? string.Empty;
    }
}
