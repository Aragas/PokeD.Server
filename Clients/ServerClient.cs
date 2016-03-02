using System;
using System.Globalization;
using Aragas.Core.Data;
using Aragas.Core.Packets;

using PokeD.Core.Packets.P3D.Shared;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients
{
    public class ServerClient : IClient
    {
        public int ID { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public string Name => "SERVER";
        public Prefix Prefix { get { throw new NotSupportedException(); } }
        public string PasswordHash { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public Vector3 Position { get { throw new NotSupportedException(); } }
        public string LevelFile { get { throw new NotSupportedException(); } }
        public string IP { get { throw new NotSupportedException(); } }
        public DateTime ConnectionTime { get { throw new NotSupportedException(); } }
        public CultureInfo Language { get; }
        public GameDataPacket GetDataPacket() { throw new NotSupportedException(); }

        public void SendPacket(ProtobufPacket packet, int originID = 0) { throw new NotSupportedException(); }

        public void LoadFromDB(Player data) { throw new NotSupportedException(); }

        public void Update() { throw new NotSupportedException(); }

        public void Dispose() { throw new NotSupportedException(); }
    }
}
