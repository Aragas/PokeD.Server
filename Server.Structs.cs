using PokeD.Core.Interfaces;
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
        private struct PlayerPacket
        {
            public IClient Player { get; }
            public Packet Packet { get; }
            public int OriginID { get; }

            public PlayerPacket(IClient player, ref Packet packet, int originID = -1)
            {
                Player = player;
                Packet = packet;
                OriginID = originID;
            }
        }
        
        private struct OriginPacket
        {
            public Packet Packet { get; }
            public int OriginID { get; }

            public OriginPacket(ref Packet packet, int origin)
            {
                Packet = packet;
                OriginID = origin;
            }
        }
    }
}
