using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.SCON;
using PokeD.Server.Clients;

namespace PokeD.Server.Extensions
{
    public static class IEnumarableClientExtensions
    {
        /// <summary>
        /// Get all connected Client Names.
        /// </summary>
        /// <returns>Returns null if there are no Client connected.</returns>
        public static IEnumerable<PlayerInfo> ClientInfos(this IEnumerable<Client> clients) =>
            clients.Where(client => !string.IsNullOrEmpty(client.IP)).Select(client => new PlayerInfo
            {
                Name = client.Name,
                IP = client.IP,
                Ping = 0,
                Position = client.Position,
                LevelFile = client.LevelFile,
                PlayTime = DateTime.Now - client.ConnectionTime
            });
    }
}