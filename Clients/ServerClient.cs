using System;
using System.Globalization;

using Aragas.Core.Data;
using Aragas.Core.Packets;

using PokeD.Core.Packets.P3D.Shared;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients
{
    public class ServerClient : Client
    {
        public override int ID { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override string Name { get { return "SERVER"; } protected set { throw new NotImplementedException(); } }

        public override Prefix Prefix
        {
            get { throw new NotSupportedException(); }
            protected set { throw new NotSupportedException(); }
        }

        public override string PasswordHash { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override Vector3 Position { get { throw new NotSupportedException(); } protected set { throw new NotSupportedException(); } }
        public override string LevelFile { get { throw new NotSupportedException(); } protected set { throw new NotSupportedException(); } }
        public override string IP { get { throw new NotSupportedException(); } }
        public override DateTime ConnectionTime { get { throw new NotSupportedException(); } }
        public override CultureInfo Language { get; }
        public override GameDataPacket GetDataPacket() { throw new NotSupportedException(); }

        public override void SendPacket(Packet packet) { throw new NotSupportedException(); }

        public override void LoadFromDB(Player data) { throw new NotSupportedException(); }

        public override void Update() { throw new NotSupportedException(); }

        public override void Dispose() { throw new NotSupportedException(); }
    }
}
