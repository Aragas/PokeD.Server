using System.Globalization;

using Aragas.Core.Wrappers;

using PokeD.Core.Extensions;

using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Extensions;

namespace PokeD.Server.DatabaseData
{
    public sealed class Player : DatabaseTable<int>
    {
        [AutoIncrement]
        public override int Id { get; protected set; }


        public Prefix Prefix{ get; private set; }
		public string Name{ get; private set; }
        public string PasswordHash { get; private set; }

        public string Position{ get; private set; }
		public string LevelFile{ get; private set; }

		public string LastIP{ get; private set; }
		public int LastConnectionTime{ get; private set; }


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
