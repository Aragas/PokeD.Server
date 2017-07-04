using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;

using Aragas.Network.Data;
using Aragas.Network.Packets;

using PokeD.Core.Data.P3D;
using PokeD.Core.Extensions;
using PokeD.Core.IO;
using PokeD.Core.Packets.P3D;
using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Client;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;
using PokeD.Server.Chat;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer : Client<ModuleP3D>
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region P3D Values

        public override int ID { get; set; }

        public string GameMode { get; private set; }
        public bool IsGameJoltPlayer { get; private set; }
        public long GameJoltID { get; private set; }
        private char DecimalSeparator { get; set; }


        public override string Nickname { get; protected set; }


        public override string LevelFile { get; set; }
        public override Vector3 Position { get; set; }
        public int Facing { get; private set; }
        public bool Moving { get; private set; }

        public string Skin { get; private set; }
        public string BusyType { get; private set; }

        public bool PokemonVisible { get; private set; }
        public Vector3 PokemonPosition { get; private set; }
        public string PokemonSkin { get; private set; }
        public int PokemonFacing { get; private set; }

        #endregion P3D Values

        #region Values

        public override Prefix Prefix { get; protected set; }
        public override string PasswordHash { get; set; }

        public override string IP => Stream.Host;

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language => new CultureInfo("en");
        public override PermissionFlags Permissions { get; set; } = PermissionFlags.UnVerified;

        bool IsInitialized { get; set; }

        #endregion Values

        private P3DTransmission Stream { get; }


#if DEBUG
        // -- Debug -- //
        private List<P3DPacket> Received { get; } =  new List<P3DPacket>();
        private List<P3DPacket> Sended { get; } = new List<P3DPacket>();
        // -- Debug -- //
#endif
        public P3DPlayer(Socket socket, ModuleP3D module) : base(module) { Stream = new P3DTransmission(socket, typeof(P3DPacketTypes)); }

        public override void Update()
        {
            while (!UpdateToken.IsCancellationRequested)
            {
                if (Stream.IsConnected)
                {
                    if (Stream.TryReadPacket(out var packet))
                    {
                        HandlePacket(packet);

#if DEBUG
                        Received.Add(packet);
#endif
                    }
                }
                else
                    break;
            }

            Leave();
        }

        private void HandlePacket(P3DPacket packet)
        {
            switch ((P3DPacketTypes) packet.ID)
            {
                case P3DPacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;

                case P3DPacketTypes.ChatMessagePrivate:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case P3DPacketTypes.ChatMessageGlobal:
                    HandleChatMessage((ChatMessageGlobalPacket) packet);
                    break;

                case P3DPacketTypes.Ping:
                    break;

                case P3DPacketTypes.GameStateMessage:
                    HandleGameStateMessage((GameStateMessagePacket) packet);
                    break;


                case P3DPacketTypes.TradeRequest:
                    HandleTradeRequest((TradeRequestPacket) packet);
                    break;

                case P3DPacketTypes.TradeJoin:
                    HandleTradeJoin((TradeJoinPacket) packet);
                    break;

                case P3DPacketTypes.TradeQuit:
                    HandleTradeQuit((TradeQuitPacket) packet);
                    break;

                case P3DPacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;

                case P3DPacketTypes.TradeStart:
                    HandleTradeStart((TradeStartPacket) packet);
                    break;


                case P3DPacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;

                case P3DPacketTypes.BattleJoin:
                    HandleBattleJoin((BattleJoinPacket) packet);
                    break;

                case P3DPacketTypes.BattleQuit:
                    HandleBattleQuit((BattleQuitPacket) packet);
                    break;

                case P3DPacketTypes.BattleOffer:
                    HandleBattleOffer((BattleOfferPacket) packet);
                    break;

                case P3DPacketTypes.BattleStart:
                    HandleBattleStart((BattleStartPacket) packet);
                    break;

                case P3DPacketTypes.BattleClientData:
                    HandleBattleClientData((BattleClientDataPacket) packet);
                    break;

                case P3DPacketTypes.BattleHostData:
                    HandleBattleHostData((BattleHostDataPacket) packet);
                    break;

                case P3DPacketTypes.BattlePokemonData:
                    HandleBattlePokemonData((BattlePokemonDataPacket) packet);
                    break;


                case P3DPacketTypes.ServerDataRequest:
                    HandleServerDataRequest((ServerDataRequestPacket) packet);
                    break;
            }
        }


        public override bool RegisterOrLogIn(string passwordHash)
        {
            if (base.RegisterOrLogIn(passwordHash))
            {
                Initialize();
                return true;
            }

            return false;
        }

        public override void SendPacket(Packet packet)
        {
            var p3dPacket = packet as P3DPacket;
            if(p3dPacket == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            Stream.SendPacket(p3dPacket);

#if DEBUG
            Sended.Add(p3dPacket);
#endif
        }
        public override void SendChatMessage(ChatChannelMessage chatMessage) { SendPacket(new ChatMessageGlobalPacket { Origin = chatMessage.ChatMessage.Sender.ID, Message = chatMessage.ChatMessage.Message }); }
        public override void SendServerMessage(string text) { SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = text }); }
        public override void SendPrivateMessage(ChatMessage chatMessage) { SendPacket(new ChatMessagePrivatePacket { Origin = chatMessage.Sender.ID, DataItems = new DataItems(chatMessage.Message) }); }

        public override void SendKick(string reason = "")
        {
            SendPacket(new KickedPacket { Origin = -1, Reason = reason });
            base.SendKick(reason);
        }
        public override void SendBan(BanTable banTable)
        {
            SendKick($"You have banned from this server; Reason: {banTable.Reason} Time left: {(banTable.UnbanTime - DateTime.UtcNow):%m} minutes; If you want to appeal your ban, please contact a staff member on the official forums (http://pokemon3d.net/forum/news/) or on the official Discord server (https://discord.me/p3d).");
            base.SendBan(banTable);
        }


        public override void Load(ClientTable data)
        {
            base.Load(data);

            Prefix = data.Prefix;
            Permissions = Permissions == PermissionFlags.UnVerified ? data.Permissions | PermissionFlags.UnVerified : data.Permissions;
            PasswordHash = data.PasswordHash;
        }


        private void Initialize()
        {
            if (!IsInitialized)
            {
                if ((Permissions & PermissionFlags.UnVerified) != PermissionFlags.None)
                    Permissions ^= PermissionFlags.UnVerified;

                if ((Permissions & PermissionFlags.User) == PermissionFlags.None)
                    Permissions |= PermissionFlags.User;

                Join();
                IsInitialized = true;
            }
        }

        private DataItems GenerateDataItems()
        {
            return new DataItems(
                GameMode,
                IsGameJoltPlayer ? "1" : "0",
                GameJoltID.ToString(CultureInfo),
                DecimalSeparator.ToString(),
                Name,
                LevelFile,
                Position.ToP3DString(DecimalSeparator, CultureInfo),
                Facing.ToString(CultureInfo),
                Moving ? "1" : "0",
                Skin,
                BusyType,
                PokemonVisible ? "1" : "0",
                PokemonPosition.ToP3DString(DecimalSeparator, CultureInfo),
                PokemonSkin,
                PokemonFacing.ToString(CultureInfo));
        }
        public override GameDataPacket GetDataPacket() => new GameDataPacket { DataItems = GenerateDataItems() };


        public override void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }
    }
}