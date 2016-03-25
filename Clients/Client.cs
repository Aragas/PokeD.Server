using System;
using System.Globalization;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;
using Aragas.Core.Packets;

using PokeD.Core.Packets.P3D.Shared;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients
{
    public abstract class Client : IUpdatable, IDisposable
    {
        public abstract int ID { get; set; }

        public abstract string Name { get; protected set; }
        public abstract Prefix Prefix { get; protected set; }
        public abstract string PasswordHash { get; set; }
        public abstract Vector3 Position { get; protected set; }
        public abstract string LevelFile { get; protected set; }

        public abstract string IP { get; }
        public abstract DateTime ConnectionTime { get; }
        public abstract CultureInfo Language { get; }


        public abstract GameDataPacket GetDataPacket();

        public abstract void SendPacket(Packet packet);
        public abstract void SendMessage(string text);

        public abstract void LoadFromDB(Player data);

        public abstract void Update();
        public abstract void Dispose();
    }
}
