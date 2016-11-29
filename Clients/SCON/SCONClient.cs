using System;
using System.Collections.Generic;
using System.Globalization;

using Aragas.Network.Data;
using Aragas.Network.IO;
using Aragas.Network.Packets;

using PCLExt.Network;

using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Lua;
using PokeD.Core.Packets.SCON.Status;
using PokeD.Server.Chat;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient : Client<ModuleSCON>
    {
        #region P3D Values

        public override int ID { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override string Nickname { get { throw new NotSupportedException(); } protected set { throw new NotSupportedException(); } }
        
        public override string LevelFile { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override Vector3 Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        #endregion P3D Values

        #region Values

        public override Prefix Prefix { get { throw new NotSupportedException(); } protected set { throw new NotSupportedException(); } }

        public override string PasswordHash { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public override string IP => Stream.Host;

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language { get; }
        public override PermissionFlags Permissions { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        bool EncryptionEnabled => Module.EncryptionEnabled;

        bool IsInitialized { get; set; }

        bool ChatEnabled { get; set; }

        #endregion Values

        ProtobufStream Stream { get; }

#if DEBUG
        // -- Debug -- //
        List<ProtobufPacket> Received { get; } = new List<ProtobufPacket>();
        List<ProtobufPacket> Sended { get; } = new List<ProtobufPacket>();
        // -- Debug -- //
#endif

        public SCONClient(ITCPClient clientWrapper, ModuleSCON module) : base(module)
        {
            Stream = new ProtobufStream(clientWrapper);

            AuthorizationStatus = (EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0);
        }

        public override void Update()
        {
            if (Stream.IsConnected)
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

                    Func<SCONPacket> func;
                    if (SCONPacketResponses.TryGetPacketFunc(id, out func))
                    {
                        if (func != null)
                        {
                            var packet = func().ReadPacket(reader);
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


        public override bool RegisterOrLogIn(string password) => false;
        public override bool ChangePassword(string oldPassword, string newPassword) => false;

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
        public override void SendChatMessage(ChatMessage chatMessage) { }
        public override void SendServerMessage(string text) { }
        public override void SendPrivateMessage(ChatMessage chatMessage) { }

        public override void Kick(string reason = "")
        {
            SendPacket(new AuthorizationDisconnectPacket { Reason = reason });

            base.Kick(reason);
        }
        public override void Ban(string reason = "")
        {
            SendPacket(new AuthorizationDisconnectPacket { Reason = reason });

            base.Ban(reason);
        }


        public override void LoadFromDB(ClientTable data) { }


        public override void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }
    }
}