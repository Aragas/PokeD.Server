using System;
using System.Globalization;

using Aragas.Network.Data;
using Aragas.Network.Packets;

using PokeD.Core;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Server.Chat;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Commands;

namespace PokeD.Server.Clients
{
    public abstract class Client : IUpdatable, IDisposable
    {
        public abstract int ID { get; set; }

        public string Name => Prefix != Prefix.NONE ? $"[{Prefix}] {Nickname}" : Nickname;
        public abstract string Nickname { get; protected set; }
        public abstract Prefix Prefix { get; protected set; }
        public abstract string PasswordHash { get; set; }
        public abstract Vector3 Position { get; set; }
        public abstract string LevelFile { get; set; }

        public abstract PermissonFlags Permissions { get; set; }
        public abstract string IP { get; }
        public abstract DateTime ConnectionTime { get; }
        public abstract CultureInfo Language { get; }


        public abstract GameDataPacket GetDataPacket();

        public abstract void SendPacket(Packet packet);
        public abstract void SendChatMessage(ChatMessage chatMessage);
        public abstract void SendServerMessage(string text);

        public abstract void LoadFromDB(ClientTable data);

        public abstract void Update();
        public abstract void Dispose();
    }
}