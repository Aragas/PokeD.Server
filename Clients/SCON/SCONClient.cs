using System;
using System.Collections.Generic;

using Aragas.Core.Data;
using Aragas.Core.IO;
using Aragas.Core.Packets;
using Aragas.Core.Wrappers;

using PokeD.Core.Packets;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Lua;
using PokeD.Core.Packets.SCON.Status;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient : IClient
    {
        #region P3D Values

        public int ID { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public string Name { get { throw new NotImplementedException(); } }


        public string LevelFile { get { throw new NotImplementedException(); } }
        public Vector3 Position { get { throw new NotImplementedException(); } }

        #endregion P3D Values

        #region Values

        public Prefix Prefix { get { throw new NotImplementedException(); } }
        public string PasswordHash { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public string IP => Stream.Host;

        public DateTime ConnectionTime { get; } = DateTime.Now;

        bool EncryptionEnabled => Module.EncryptionEnabled;

        bool IsInitialized { get; set; }

        bool ChatEnabled { get; set; }

        #endregion Values

        ProtobufStream Stream { get; }

        ModuleSCON Module { get; }

#if DEBUG
        // -- Debug -- //
        List<SCONPacket> Received { get; } = new List<SCONPacket>();
        List<SCONPacket> Sended { get; } = new List<SCONPacket>();
        // -- Debug -- //
#endif

        public SCONClient(ITCPClient clientWrapper, IServerModule server)
        {
            Stream = new ProtobufStream(clientWrapper);
            Module = (ModuleSCON) server;

            AuthorizationStatus = (EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0);
        }

        public void Update()
        {
            if (Stream.Connected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var dataLength = Stream.ReadVarInt();
                    if (dataLength != 0)
                    {
                        var data = Stream.ReadByteArray(dataLength);

                        HandleData(data);
                    }
                    else
                    {
                        Logger.Log(LogType.Error, $"Protobuf Reading Error: Packet Length size is 0. Disconnecting.");
                        Module.RemoveClient(this, "Packet Length size is 0!");
                    }
                }
            }
            else
                Module.RemoveClient(this);
        }

        private void HandleData(byte[] data)
        {
            if (data != null)
            {
                using (PacketDataReader reader = new ProtobufDataReader(data))
                {
                    var id = reader.Read<VarInt>();

                    if (SCONPacketResponses.Packets.Length > id)
                    {
                        if (SCONPacketResponses.Packets[id] != null)
                        {
                            var packet = SCONPacketResponses.Packets[id]().ReadPacket(reader) as SCONPacket;
                            if (packet != null)
                            {
                                HandlePacket(packet);

#if DEBUG
                                Received.Add(packet);
#endif
                            }
                            else
                                Logger.Log(LogType.Error, $"SCON Reading Error: packet is null. Packet ID {id}"); // TODO: Disconnect?
                        }
                        else
                            Logger.Log(LogType.Error, $"SCON Reading Error: SCONPacketResponses.Packets[{id}] is null.");
                    }
                    else
                    {
                        Logger.Log(LogType.Error, $"SCON Reading Error: Packet ID {id} is not correct, Packet Data: {data}. Disconnecting.");
                        Module.RemoveClient(this, $"Packet ID {id} is not correct!");
                    }
                }
            }
            else
                Logger.Log(LogType.Error, $"SCON Reading Error: Packet Data is null.");
        }
        private void HandlePacket(ProtobufPacket packet)
        {
            switch ((SCONPacketTypes)(int) packet.ID)
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


                case SCONPacketTypes.UploadLuaToServer:
                    HandleUploadLuaToServer((UploadLuaToServerPacket) packet);
                    break;

                case SCONPacketTypes.ReloadNPCs:
                    HandleReloadNPCs((ReloadNPCsPacket) packet);
                    break;
            }
        }


        public GameDataPacket GetDataPacket() { throw new NotImplementedException(); }

        public void SendPacket(ProtobufPacket packet, int originID = 0)
        {
            var sconPacket = packet as SCONPacket;
            if (sconPacket == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            if (packet is ChatMessagePacket)
                if(!ChatEnabled)
                    return;
            
            Stream.SendPacket(ref packet);
     
#if DEBUG
            Sended.Add(sconPacket);
#endif
        }

        public void LoadFromDB(Player data) { }


        public void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }
    }
}