using System;
using System.Collections.Generic;
using System.Globalization;

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
using PokeD.Server.DatabaseData;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient : Client
    {
        #region P3D Values

        public override int ID { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        public override string Name { get { throw new NotImplementedException(); } protected set { throw new NotImplementedException(); } }
        
        public override string LevelFile { get { throw new NotImplementedException(); } protected set { throw new NotImplementedException(); } }
        public override Vector3 Position { get { throw new NotImplementedException(); } protected set { throw new NotImplementedException(); } }

        #endregion P3D Values

        #region Values

        public override Prefix Prefix { get { throw new NotImplementedException(); } protected set { throw new NotImplementedException(); } }

        public override string PasswordHash { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public override string IP => Stream.Host;

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language { get; }

        bool EncryptionEnabled => Module.EncryptionEnabled;

        bool IsInitialized { get; set; }

        bool ChatEnabled { get; set; }

        #endregion Values

        ProtobufStream Stream { get; }

        ModuleSCON Module { get; }

#if DEBUG
        // -- Debug -- //
        List<ProtobufPacket> Received { get; } = new List<ProtobufPacket>();
        List<ProtobufPacket> Sended { get; } = new List<ProtobufPacket>();
        // -- Debug -- //
#endif

        public SCONClient(ITCPClient clientWrapper, ModuleSCON module)
        {
            Stream = new ProtobufStream(clientWrapper);
            Module = module;

            AuthorizationStatus = (EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0);
        }

        public override void Update()
        {
            if (Stream.Connected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var dataLength = Stream.ReadVarInt();
                    if (dataLength != 0)
                    {
                        var data = Stream.Receive(dataLength);

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
                using (var reader = new ProtobufDataReader(data))
                {
                    var id = reader.Read<VarInt>();

                    if (SCONPacketResponses.Packets.Length > id)
                    {
                        if (SCONPacketResponses.Packets[id] != null)
                        {
                            var packet = SCONPacketResponses.Packets[id]().ReadPacket(reader);
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
            switch ((SCONPacketTypes) (int) packet.ID)
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


        public override GameDataPacket GetDataPacket() { throw new NotSupportedException(); }

        public override void SendPacket(Packet packet)
        {
            var sconPacket = packet as SCONPacket;
            if (sconPacket == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            if (packet is ChatMessagePacket)
                if(!ChatEnabled)
                    return;
            
            Stream.SendPacket(packet);
     
#if DEBUG
            Sended.Add(sconPacket);
#endif
        }
        public override void SendMessage(string text) { }

        public override void LoadFromDB(Player data) { }


        public override void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }
    }
}