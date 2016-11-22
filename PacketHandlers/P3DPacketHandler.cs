using System;

using Aragas.Network.Extensions;
using Aragas.Network.PacketHandlers;

using PCLExt.AppDomain;

using PokeD.Core.Packets.P3D;

namespace PokeD.Server.PacketHandlers
{
    // Not used.
    internal static class P3DPacketHandler
    {
        internal static class ClientPacketResponses
        {
            internal static readonly Func<IPacketHandlerContext, ContextFunc<P3DPacket>>[] Handlers;

            static ClientPacketResponses()
            {
                new P3DPacketTypes().CreateHandlerInstancesOut(out Handlers, AppDomain.GetAssembly(typeof(ClientPacketResponses)));
            }
        }

        internal static class ServerPacketResponses
        {
            internal static readonly Func<IPacketHandlerContext, ContextFunc<P3DPacket>>[] Handlers;

            static ServerPacketResponses()
            {
                new P3DPacketTypes().CreateHandlerInstancesOut(out Handlers, AppDomain.GetAssembly(typeof(ServerPacketResponses)));
            }
        }
    }
}