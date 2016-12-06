using System.Collections.Generic;
using System.Linq;

using PokeD.Server.Clients;
using PokeD.Server.Commands;

namespace PokeD.Server
{
    public partial class Server
    {
        /// <summary>
        /// Get all connected <see cref="Client"/>.
        /// </summary>
        /// <returns>Returns <see langword="null"/> if there are no <see cref="Client"/> connected.</returns>
        public IEnumerable<Client> GetAllClients() => Modules.Where(module => module.ClientsVisible)
            .SelectMany(module => module.Clients.Where(client => !client.Permissions.HasFlag(PermissionFlags.UnVerified)));

        /// <summary>
        /// Get <see cref="Client"/> by <paramref name="id"/>.
        /// </summary>
        /// <param name="id"><see cref="Client.Id"/></param>
        /// <returns>Returns <see langword="null"/> if <see cref="Client"/> was not found.</returns>
        public Client GetClient(int id) => GetAllClients().FirstOrDefault(client => client.Id == id);

        /// <summary>
        /// Get <see cref="Client"/> by <paramref name="name"/>.
        /// </summary>
        /// <param name="name"><see cref="Client.Nickname"/></param>
        /// <returns>Returns <see langword="null"/> if <see cref="Client"/> was not found.</returns>
        public Client GetClient(string name) => GetAllClients().FirstOrDefault(client => client.Nickname == name);

        /// <summary>
        /// Get <see cref="Client"/> <see cref="Client.Id"/> by <paramref name="name"/>.
        /// </summary>
        /// <param name="name"><see cref="Client.Nickname"/></param>
        /// <returns>Returns <see cref="-1"/> if <see cref="Client"/> was not found.</returns>
        public int GetClientId(string name) => GetClient(name)?.Id ?? -1;

        /// <summary>
        /// Get <see cref="Client"/> <see cref="Client.Nickname"/> by <paramref name="id"/>.
        /// </summary>
        /// <param name="id"><see cref="Client.Id"/></param>
        /// <returns>Returns <see cref="string.Empty"/> if <see cref="Client"/> was not found.</returns>
        public string GetClientName(int id) => GetClient(id)?.Nickname ?? string.Empty;
    }
}