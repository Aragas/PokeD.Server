using PokeD.Core.Packets;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public enum MuteStatus
    {
        Completed,
        PlayerNotFound,
        MutedYourself,
        IsNotMuted
    }

    public partial class Server
    {
        private struct PlayerPacketP3DOrigin
        {
            public readonly IClient Player;
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public PlayerPacketP3DOrigin(IClient player, ref P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
            public PlayerPacketP3DOrigin(IClient player, P3DPacket packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        }
        
        private struct PacketP3DOrigin
        {
            public readonly P3DPacket Packet;
            public readonly int OriginID;

            public PacketP3DOrigin(ref P3DPacket packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}
