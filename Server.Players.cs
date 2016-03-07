using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.SCON;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public partial class Server
    {
        /// <summary>
        /// Get all connected Client.
        /// </summary>
        /// <returns>Returns null if there are no Client connected.</returns>
        public IEnumerable<Client> AllClients() => Modules.Where(module => module.ClientsVisible).SelectMany(module => module.Clients);

        /// <summary>
        /// Get all connected Client Names.
        /// </summary>
        /// <returns>Returns null if there are no Client connected.</returns>
        public IEnumerable<PlayerInfo> GetAllClientsInfo() => Modules.Where(module => module.ClientsVisible).SelectMany(module => module.Clients.GetAllClientsInfo());


        /// <summary>
        /// Get Client by ID.
        /// </summary>
        /// <param name="id">Client ID.</param>
        /// <returns>Returns null if Client is not found.</returns>
        public Client GetClient(int id) => AllClients().FirstOrDefault(client => client.ID == id);

        /// <summary>
        /// Get Client by name.
        /// </summary>
        /// <param name="name">Client Name.</param>
        /// <returns>Returns null if Client is not found.</returns>
        public Client GetClient(string name) => AllClients().FirstOrDefault(client => client.Name == name);

        /// <summary>
        /// Get Client Name by ID.
        /// </summary>
        /// <param name="name">Client Name.</param>
        /// <returns>Returns -1 if Client is not found.</returns>
        public int GetClientID(string name) => GetClient(name)?.ID ?? -1;

        /// <summary>
        /// Get Client ID by Name.
        /// </summary>
        /// <param name="id">Client ID.</param>
        /// <returns>Returns <see cref="string.Empty"/> if Client is not found.</returns>
        public string GetClientName(int id) => GetClient(id)?.Name ?? string.Empty;
    }
}
