using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Status;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Wrappers;

using PokeD.Server.Exceptions;
using PokeD.Server.IO;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient : IClient
    {
        #region Values

        [JsonIgnore]
        public int ID { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        [JsonIgnore]
        public string Name { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public string IP => Client.IP;

        [JsonIgnore]
        public DateTime ConnectionTime { get; } = DateTime.Now;

        [JsonIgnore]
        public bool UseCustomWorld { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public long GameJoltId { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public bool IsGameJoltPlayer { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public string LevelFile { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public Vector3 Position { get { throw new NotImplementedException(); } }

        [JsonProperty("ChatReceiving")]
        public bool ChatReceiving { get; private set; }

        #endregion Values

        INetworkTCPClient Client { get; }
        IPacketStream Stream { get; }

        readonly Server _server;

#if DEBUG
        // -- Debug -- //
        List<ProtobufPacket> Received { get; } = new List<ProtobufPacket>();
        List<ProtobufPacket> Sended { get; } = new List<ProtobufPacket>();
        // -- Debug -- //
#endif

        public SCONClient(INetworkTCPClient client, Server server)
        {
            Client = client;
            Stream = new ProtobufStream(Client);
            _server = server;

            AuthorizationStatus = 
                (_server.SCONEnabled ? AuthorizationStatus.RemoteClientEnabled : 0)     | 
                (_server.EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0);
        }

        public void Update()
        {
            if (Stream.Connected && Stream.DataAvailable > 0)
            {
                var packetLength = Stream.ReadVarInt();
                if (packetLength == 0)
                    throw new SCONException("Reading error: Packet Length size is 0.");

                var packetId = Stream.ReadVarInt();

                var data = Stream.ReadByteArray(packetLength - 1);


                HandleData(packetId, data);
            }
        }

        /// <summary>
        /// Data is handled here.
        /// </summary>
        /// <param name="id">Packet ID</param>
        /// <param name="data">Packet byte[] data</param>
        private void HandleData(int id, byte[] data)
        {
            if (data == null)
                return;

            using (var reader = new ProtobufDataReader(data))
            {
                if (SCONResponse.Packets[id] == null)
                    throw new SCONException("Reading error: Wrong packet ID.");

                var packet = SCONResponse.Packets[id]().ReadPacket(reader);


                HandlePacket(packet);
#if DEBUG
                Received.Add(packet);
#endif
            }
        }

        /// <summary>
        /// Packets are handled here.
        /// </summary>
        /// <param name="packet">Packet</param>
        private void HandlePacket(ProtobufPacket packet)
        {
            switch ((SCONPacketTypes) packet.ID)
            {
                case SCONPacketTypes.AuthorizationRequest:
                    HandleAuthorizationRequest((AuthorizationRequestPacket) packet);
                    break;


                case SCONPacketTypes.EncryptionResponse:
                    HandleEncryptionResponse((EncryptionResponsePacket) packet);
                    break;
                    

                case SCONPacketTypes.AuthorizationPassword:
                    HandleAuthorizationPassword((AuthorizationPasswordPacket) packet);
                    break;

                    
                case SCONPacketTypes.ExecuteCommand:
                    HandleExecuteCommand((ExecuteCommandPacket) packet);
                    break;
                    

                case SCONPacketTypes.PlayerListRequest:
                    HandlePlayerListRequest((PlayerListRequestPacket) packet);
                    break;
                    

                case SCONPacketTypes.StartChatReceiving:
                    HandleStartChatReceiving((StartChatReceivingPacket) packet);
                    break;

                case SCONPacketTypes.StopChatReceiving:
                    HandleStopChatReceiving((StopChatReceivingPacket) packet);
                    break;
                    

                case SCONPacketTypes.PlayerLocationRequest:
                    HandlePlayerLocationRequest((PlayerLocationRequestPacket) packet);
                    break;


                case SCONPacketTypes.LogListRequest:
                    HandleLogListRequest((LogListRequestPacket) packet);
                    break;

                case SCONPacketTypes.LogFileRequest:
                    HandleLogFileRequest((LogFileRequestPacket) packet);
                    break;


                case SCONPacketTypes.CrashLogListRequest:
                    HandleCrashLogListRequest((CrashLogListRequestPacket) packet);
                    break;

                case SCONPacketTypes.CrashLogFileRequest:
                    HandleCrashLogFileRequest((CrashLogFileRequestPacket) packet);
                    break;
            }
        }


        public GameDataPacket GetDataPacket()
        {
            throw new NotImplementedException();
        }
        public DataItems GenerateDataItems()
        {
            throw new NotImplementedException();
        }


        public void SendPacket(ProtobufPacket packet, int originID = 0)
        {
            if (Stream.Connected)
            {
                Stream.SendPacket(ref packet);

#if DEBUG
                Sended.Add(packet);
#endif
            }
        }
        public void SendPacket(P3DPacket packet, int originID = 0)
        {
            // TODO: Nope.
            if (Stream.Connected)
            {
                var messagePacket = packet as Core.Packets.Chat.ChatMessagePacket;
                if (messagePacket != null)
                    SendPacket(new ChatMessagePacket { Player = _server.GetClientName(messagePacket.Origin), Message = messagePacket.Message });
                
#if DEBUG
                Sended.Add(packet);
#endif
            }
        }


        public void Dispose()
        {
            Stream?.Dispose();

            _server.RemovePlayer(this);
        }
    }
}