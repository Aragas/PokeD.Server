/*
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;

using Aragas.Network.Data;
using Aragas.Network.IO;
using Aragas.Network.Packets;

using PokeD.Core.Data;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.SCON;
using PokeD.Core.Packets.SCON.Authorization;
using PokeD.Core.Packets.SCON.Chat;
using PokeD.Core.Packets.SCON.Logs;
using PokeD.Core.Packets.SCON.Script;
using PokeD.Core.Packets.SCON.Status;
using PokeD.Server.Chat;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;

namespace PokeD.Server.Clients.SCON
{
    public partial class SCONClient : Client<ModuleSCON>
    {
        #region P3D Values

        public override int ID { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override string Nickname { get => throw new NotSupportedException(); protected set => throw new NotSupportedException(); }

        public override string LevelFile { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override Vector3 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        #endregion P3D Values

        #region Values

        public override Prefix Prefix { get => throw new NotSupportedException(); protected set => throw new NotSupportedException(); }

        public override string PasswordHash { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override string IP => Stream.Host;

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language { get; }
        public override PermissionFlags Permissions { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        private bool EncryptionEnabled => Module.EncryptionEnabled;

        private bool IsInitialized { get; set; }

        private bool ChatEnabled { get; set; }

        #endregion Values

        private Socket Socket { get; }
        private BasePacketFactory<SCONPacket, VarInt, ProtobufSerializer, ProtobufDeserialiser> PacketFactory { get; }
        private ProtobufTransmission<SCONPacket> Stream { get; set; }

#if DEBUG
        // -- Debug -- //
        private const int QueueSize = 1000;
        private Queue<SCONPacket> Received { get; } = new Queue<SCONPacket>(QueueSize);
        private Queue<SCONPacket> Sended { get; } = new Queue<SCONPacket>(QueueSize);
        // -- Debug -- //
#endif

        private bool IsDisposing { get; set; }

        public SCONClient(Socket socket, ModuleSCON module) : base(module)
        {
            Socket = socket;
            PacketFactory = new PacketEnumFactory<SCONPacket, SCONPacketTypes, VarInt, ProtobufSerializer, ProtobufDeserialiser>();
            Stream = new ProtobufTransmission<SCONPacket>(Socket, new NetworkStream(Socket), PacketFactory);

            AuthorizationStatus = (EncryptionEnabled ? AuthorizationStatus.EncryprionEnabled : 0);
        }

        public override void Update()
        {
            if (Stream.IsConnected)
            {
                SCONPacket packet;
                while ((packet = Stream.ReadPacket()) != null)
                {
                    HandlePacket(packet);

#if DEBUG
                    Received.Enqueue(packet);
                    if (Received.Count >= QueueSize)
                        Received.Dequeue();
#endif
                }
            }
            else
                Leave();
        }

        private void HandlePacket(SCONPacket packet)
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


                case SCONPacketTypes.ChatReceivePacket:
                    HandleChatReceivePacket((ChatReceivePacket) packet);
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
                    HandleUploadLuaToServer((UploadScriptToServerPacket) packet);
                    break;

                case SCONPacketTypes.ReloadNPCs:
                    HandleReloadNPCs((ReloadScriptPacket) packet);
                    break;
            }
        }


        public override bool RegisterOrLogIn(string passwordHash) => false;
        public override bool ChangePassword(string oldPassword, string newPassword) => false;

        public override GameDataPacket GetDataPacket() { throw new NotSupportedException(); }

        public override void SendPacket<TPacket>(TPacket packet)
        {
            if (!(packet is SCONPacket sconPacket))
                throw new Exception($"Wrong packet type, {typeof(TPacket).FullName}");

            if (sconPacket is ChatMessagePacket)
                if (!ChatEnabled)
                    return;

            Stream.SendPacket(sconPacket);

#if DEBUG
            Sended.Enqueue(sconPacket);
            if (Sended.Count >= QueueSize)
                Sended.Dequeue();
#endif
        }
        public override void SendChatMessage(ChatChannel chatChannel, ChatMessage chatMessage) { }
        public override void SendServerMessage(string text) { }
        public override void SendPrivateMessage(ChatMessage chatMessage) { }

        public override void SendKick(string reason = "")
        {
            SendPacket(new AuthorizationDisconnectPacket { Reason = reason });
            base.SendKick(reason);
        }
        public override void SendBan(BanTable banTable)
        {
            SendPacket(new AuthorizationDisconnectPacket { Reason = $"This SCON Client was banned from the Server.\r\nTime left: {DateTime.UtcNow - banTable.UnbanTime:%m}\r\nReason: {banTable.Reason}" });
            base.SendBan(banTable);
        }

        public override void Load(ClientTable data) { }


        protected override void Dispose(bool disposing)
        {
            if (!IsDisposing)
            {
                if (disposing)
                {
                    Stream.Disconnect();
                    Stream.Dispose();

#if DEBUG
                    Sended.Clear();
                    Received.Clear();
#endif
                }


                IsDisposing = true;
            }
            base.Dispose(disposing);
        }
    }
}
*/