using System.Globalization;

using Aragas.Core.Wrappers;

using PokeD.Core.Extensions;

using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Extensions;

namespace PokeD.Server.Database
{
    public class Player : DatabaseTable
    {
		public Prefix Prefix{ get; set; }
		public string Name{ get; set; }
        public string PasswordHash { get; set; }

        public string Position{ get; set; }
		public string LevelFile{ get; set; }

		public string LastIP{ get; set; }
		public int LastConnectionTime{ get; set; }


        public Player() { }

        public Player(Client client)
        {
            if (client.ID >= 0)
                Id = client.ID;

            Prefix = client.Prefix;
            Name = client.Name;
            PasswordHash = client.PasswordHash;

            Position = client.Position.ToPokeString(',', CultureInfo.InvariantCulture);
            LevelFile = client.LevelFile;

            LastIP = client.IP;

            LastConnectionTime = client.ConnectionTime.ToUnixTime();
        }
    }
}
