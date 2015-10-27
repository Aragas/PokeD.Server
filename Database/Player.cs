using System;
using System.Globalization;

using Aragas.Core.Wrappers;

using PokeD.Core.Extensions;

using PokeD.Server.Clients;
using PokeD.Server.Data;

namespace PokeD.Server.Database
{
    public enum PlayerType { Player = 0, NPC = 1 }

    public class Player : DatabaseTable
    {
		public PlayerType PlayerType { get; set; }

		public long GameJoltID{ get; set; }

		public Prefix Prefix{ get; set; }
		public string Name{ get; set; }

		public string Position{ get; set; }
		public string LevelFile{ get; set; }

		public string LastIP{ get; set; }
		public DateTime LastConnectionTime{ get; set; }

		public bool IsUsingCustomWorld{ get; set; }

        //public int[] MutedPlayers;


        public Player() { }

        public Player(IClient client, PlayerType type)
        {
            PlayerType = type;

            if (client.ID >= 0)
                Id = client.ID;

            GameJoltID = client.IsGameJoltPlayer ? client.GameJoltID : 0;

            Prefix = client.Prefix;
            Name = client.Name;

            Position = client.Position.ToPokeString(',', CultureInfo.InvariantCulture);
            LevelFile = client.LevelFile;

            LastIP = client.IP;
            LastConnectionTime = client.ConnectionTime;

            IsUsingCustomWorld = client.UseCustomWorld;
        }
    }


    public class PlayerFull : DatabaseTable
    {
        public int OID { get; set; }

        public PlayerType PlayerType { get; set; }

        public long GameJoltID { get; set; }

        public Prefix Prefix { get; set; }
        public string Name { get; set; }

        public string Position { get; set; }
        public string LevelFile { get; set; }

        public string LastIP { get; set; }
        public DateTime LastConnectionTime { get; set; }

        public bool IsUsingCustomWorld { get; set; }

        public int[] MutedPlayers { get; set; }

        public PlayerFull() { }

        public PlayerFull(IClient client, PlayerType type)
        {
            PlayerType = type;

            if (client.ID >= 0)
                OID = client.ID;

            GameJoltID = client.IsGameJoltPlayer ? client.GameJoltID : 0;

            Prefix = client.Prefix;
            Name = client.Name;

            Position = client.Position.ToPokeString(',', CultureInfo.InvariantCulture);
            LevelFile = client.LevelFile;

            LastIP = client.IP;
            LastConnectionTime = client.ConnectionTime;

            IsUsingCustomWorld = client.UseCustomWorld;
        }
    }
}
