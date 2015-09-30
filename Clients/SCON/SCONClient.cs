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
        public ulong GameJoltID { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public bool IsGameJoltPlayer { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public string LevelFile { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        public Vector3 Position { get { throw new NotImplementedException(); } }

        [JsonProperty("ChatReceiving")]
        public bool ChatReceiving { get; private set; }

        [JsonIgnore]
        public bool IsMoving { get; private set; }

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
                (_server.SCON_Enabled ? AuthorizationStatus.RemoteClientEnabled : 0)     | 
                (_server.EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0);
        }

        public void Update()
        {
            if (!Stream.Connected)
            {
                Dispose();
                return;
            }

            if (Stream.Connected && Stream.DataAvailable > 0)
            {
                //try
                //{
                    var dataLength = Stream.ReadVarInt();
                    if (dataLength == 0)
                    {
                        Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: Packet Length size is 0. Disconnecting.");
                        SendPacket(new AuthorizationDisconnectPacket { Reason = "Packet Length size is 0!" });
                        Dispose();
                        return;
                    }

                    var data = Stream.ReadByteArray(dataLength);

                    HandleData(data);
                //}
                //catch (ProtobufReadingException ex) { Logger.Log(LogType.GlobalError, $"Protobuf Reading Exeption: {ex.Message}. Disconnecting IClient {Name}."); }
            }
        }

        private void HandleData(byte[] data)
        {
            if (data == null)
            {
                Logger.Log(LogType.GlobalError, $"SCON Reading Error: Packet Data is null.");
                return;
            }

            using (var reader = new ProtobufDataReader(data))
            {
                var id = reader.ReadVarInt();
                var origin = reader.ReadVarInt();

                if (id >= SCONResponse.Packets.Length)
                {
                    Logger.Log(LogType.GlobalError, $"SCON Reading Error: Packet ID {id} is not correct, Packet Data: {data}. Disconnecting.");
                    SendPacket(new AuthorizationDisconnectPacket {Reason = $"Packet ID {id} is not correct!"});
                    Dispose();
                    return;
                }

                var packet = SCONResponse.Packets[id]().ReadPacket(reader);
                packet.Origin = origin;

                HandlePacket(packet);

#if DEBUG
                Received.Add(packet);
#endif
            }
        }
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
                   

                case SCONPacketTypes.StartChatReceiving:
                    HandleStartChatReceiving((StartChatReceivingPacket) packet);
                    break;

                case SCONPacketTypes.StopChatReceiving:
                    HandleStopChatReceiving((StopChatReceivingPacket) packet);
                    break;


                case SCONPacketTypes.PlayerInfoListRequest:
                    HandlePlayerInfoListRequest((PlayerInfoListRequestPacket) packet);
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


                case SCONPacketTypes.PlayerDatabaseListRequest:
                    HandlePlayerDatabaseListRequest((PlayerDatabaseListRequestPacket) packet);
                    break;


                case SCONPacketTypes.BanListRequest:
                    HandleBanListRequest((BanListRequestPacket) packet);
                    break;
            }
        }


        public GameDataPacket GetDataPacket()
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


        public void Disconnect()
        {
            Stream.Disconnect();
        }
        
        public void Dispose()
        {
            Stream?.Dispose();

            _server.RemoveSCONClient(this);
        }
    }
}