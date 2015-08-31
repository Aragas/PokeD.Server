using System;
using System.IO;
using System.Threading;
using PokeD.Core.Interfaces;
using PokeD.Core.IO;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Remote.Authorization;
using PokeD.Core.Wrappers;

using PokeD.Server.Exceptions;
using PokeD.Server.IO;

namespace PokeD.Server.Data
{
    public partial class RemoteClient : IUpdatable, IDisposable
    {
        IPokeStream Stream { get; set; }

        readonly Server _server;

        public RemoteClient(INetworkTCPClient client, Server server)
        {
            Stream = new PlayerStream(client);
            _server = server;
        }

        public void Update()
        {
            int packetId = 0;
            byte[] data = null;


            if (!CompressionEnabled)
            {
                var packetLength = Stream.ReadVarInt();
                if (packetLength == 0)
                    throw new ServerException("Remote Client reading error: Packet Length size is 0");

                packetId = Stream.ReadVarInt();

                data = Stream.ReadByteArray(packetLength - 1);
            }

            HandlePacket(packetId, data);

        }

        /// <summary>
        /// Packets are handled here. Compression and encryption are handled here too
        /// </summary>
        /// <param name="id">Packet ID</param>
        /// <param name="data">Packet byte[] data</param>
        private void HandlePacket(int id, byte[] data)
        {
            using (var reader = new PlayerDataReader(data))
            {
                var packet = default(IPacket);

                if (RemoteResponse.Packets[id] == null)
                    throw new ServerException("RemoteClient eeading error: Wrong packet ID.");

                packet = RemoteResponse.Packets[id]().ReadPacket(reader);

                HandlePacket(packet);
            }
        }

        private void HandlePacket(IPacket packet)
        {
            switch ((RemotePacketTypes) packet.ID)
            {
                case RemotePacketTypes.AuthorizationRequestPacket:
                    HandleAuthorizationRequest((AuthorizationRequestPacket) packet);
                    break;

                case RemotePacketTypes.EncryptionRequestPacket:
                    HandleEncryptionRequest((EncryptionRequestPacket) packet);
                    break;

                case RemotePacketTypes.CompressionRequestPacket:
                    HandleCompressionRequest((CompressionRequestPacket) packet);
                    break;

            }
        }

        public void SendPacket(IPacket packet)
        {
            if (Stream.Connected)
                Stream.SendPacket(ref packet);
        }

        public void Dispose()
        {
            if (Stream != null)
                Stream.Dispose();


            _server.RemoveRemoteClient(this);
        }
    }
}