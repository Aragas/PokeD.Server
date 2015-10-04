using System;
using System.Collections.Generic;

using Aragas.Core.Data;
using Aragas.Core.IO;
using Aragas.Core.Packets;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Packets;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Status;
using PokeD.Core.Packets.Shared;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient : IClient
    {
        #region Values

        [JsonIgnore]
        public int ID { get { return 0; } set {  } }

        [JsonIgnore]
        public string Name => string.Empty;

        [JsonIgnore]
        public string IP => Client.IP;

        [JsonIgnore]
        public DateTime ConnectionTime { get; } = DateTime.Now;

        [JsonIgnore]
        public bool UseCustomWorld => true;

        [JsonIgnore]
        public ulong GameJoltID => 0;

        [JsonIgnore]
        public bool IsGameJoltPlayer => false;

        [JsonIgnore]
        public string LevelFile => string.Empty;

        [JsonIgnore]
        public Vector3 Position => Vector3.Zero;

        [JsonProperty("ChatReceiving")]
        public bool ChatReceiving { get; private set; }

        [JsonIgnore]
        public bool IsMoving { get; private set; }

        bool IsInitialized { get; set; }
        bool IsDisposed { get; set; }

        #endregion Values

        INetworkTCPClient Client { get; }
        ProtobufStream Stream { get; }

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
            if (Stream.Connected)
            {
                if (Stream.Connected && Stream.DataAvailable > 0)
                {
                    var dataLength = Stream.ReadVarInt();
                    if (dataLength == 0)
                    {
                        Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: Packet Length size is 0. Disconnecting.");
                        SendPacket(new AuthorizationDisconnectPacket {Reason = "Packet Length size is 0!"});
                        _server.RemovePlayer(this);
                        return;
                    }

                    var data = Stream.ReadByteArray(dataLength);

                    HandleData(data);
                }
            }
            else
                _server.RemovePlayer(this);
        }

        private void HandleData(byte[] data)
        {
            if (data != null)
            {
                using (var reader = new ProtobufDataReader(data))
                {
                    var id = reader.ReadVarInt();
                    var origin = reader.ReadVarInt();

                    if (SCONPacketResponses.Packets.Length > id)
                    {
                        if (SCONPacketResponses.Packets[id] != null)
                        {
                            var packet = SCONPacketResponses.Packets[id]().ReadPacket(reader);
                            packet.Origin = origin;

                            HandlePacket(packet);

#if DEBUG
                            Received.Add(packet);
#endif
                        }
                        else
                            Logger.Log(LogType.GlobalError, $"SCON Reading Error: SCONPacketResponses.Packets[{id}] is null.");
                    }
                    else
                    {
                        Logger.Log(LogType.GlobalError, $"SCON Reading Error: Packet ID {id} is not correct, Packet Data: {data}. Disconnecting.");
                        SendPacket(new AuthorizationDisconnectPacket {Reason = $"Packet ID {id} is not correct!"});
                        _server.RemovePlayer(this);
                    }
                }
            }
            else
                Logger.Log(LogType.GlobalError, $"SCON Reading Error: Packet Data is null.");
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


        private void SendPacket(ProtobufPacket packet, int originID = 0)
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
                var messagePacket = packet as Core.Packets.Chat.ChatMessageGlobalPacket;
                if (messagePacket != null)
                    SendPacket(new ChatMessagePacket { Player = _server.GetClientName(messagePacket.Origin), Message = messagePacket.Message });
                
#if DEBUG
                Sended.Add(packet);
#endif
            }
        }


        private void DisconnectAndDispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;


            Stream.Dispose();
        }
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;


            DisconnectAndDispose();
        }
    }
}