using System.Globalization;

using PokeD.Core.Extensions;
using PokeD.Server.Clients;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Extensions;

using SQLite;

namespace PokeD.Server.Database
{
    /// <summary>
    /// General Info.
    /// </summary>
    public sealed class ClientTable : IDatabaseTable
    {
        [PrimaryKey, AutoIncrement]
        public int? ClientID { get; set; }


        public Prefix Prefix { get; set; }
        public PermissionFlags Permissions { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }

        public string Position { get; set; }
        public string LevelFile { get; set; }

        public string LastIP { get; set; }
        public int LastConnectionTime { get; set; }


        public ClientTable(){ }
        public ClientTable(Client client)
        {
            if (client.ID > 0)
                ClientID = client.ID;

            Prefix = client.Prefix;
            Permissions = client.Permissions;
            Name = client.Nickname;
            PasswordHash = client.PasswordHash;

            Position = client.Position.ToP3DString(',', CultureInfo.InvariantCulture);
            LevelFile = client.LevelFile;

            LastIP = client.IP;

            LastConnectionTime = client.ConnectionTime.ToUnixTime();
        }
    }
}