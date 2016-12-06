using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Aragas.Network.Data;
using Aragas.Network.IO;
using Aragas.Network.Packets;

using PCLExt.Network;

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

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer : Client<ModuleP3D>
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region P3D Values

        public override int Id { get; set; }

        public string GameMode { get; private set; }
        public bool IsGameJoltPlayer { get; private set; }
        public long GameJoltId { get; private set; }
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

        P3DStream Stream { get; }


#if DEBUG
        // -- Debug -- //
        List<P3DPacket> Received { get; } =  new List<P3DPacket>();
        List<P3DPacket> Sended { get; } = new List<P3DPacket>();
        // -- Debug -- //
#endif
        private bool _event;

        public P3DPlayer(ISocketClient socket, ModuleP3D module) : base(module) { Stream = new P3DStream(socket); }
        public P3DPlayer(ISocketClientEvent socketEvent, ModuleP3D module) : base(module)
        {
            _event = true;

            Stream = new P3DStreamEvent(socketEvent);
            ((P3DStreamEvent) Stream).DataReceived += P3DPlayer_DataReceived;
            ((P3DStreamEvent) Stream).Disconnected += P3DPlayer_Disconnected;
        }
        private void P3DPlayer_DataReceived(PacketStreamDataReceivedArgs args)
        {
            HandleData(Encoding.UTF8.GetString(args.Data, 0, args.Data.Length));
        }
        private void P3DPlayer_Disconnected(PacketStreamDisconnectedArgs args)
        {
            Kick();
        }

        public override void Update()
        {
            if (_event)
                return;

            if (Stream.IsConnected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var data = Stream.ReadLine();

                    HandleData(data);
                }
            }
            else
                Kick();
        }

        private void HandleData(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                int id;
                if (P3DPacket.TryParseId(data, out id))
                {
                    Func<P3DPacket> func;
                    if (P3DPacketResponses.TryGetPacketFunc(id, out func))
                    {
                        if (func != null)
                        {
                            var packet = func();
                            if (packet.TryParseData(data))
                            {
                                HandlePacket(packet);

#if DEBUG
                                Received.Add(packet);
#endif
                            }
                            else
                                Logger.Log(LogType.Error, $"P3D Reading Error: Packet TryParseData error. Packet Id {id}, Packet Data: {data}.");
                        }
                        else
                            Logger.Log(LogType.Error, $"P3D Reading Error: SCONPacketResponses.Packets[{id}] is null.");
                    }
                    else
                        Logger.Log(LogType.Error, $"P3D Reading Error: Packet Id {id} doesn't exist, Packet Data: {data}.");
                }
                else
                    Logger.Log(LogType.Error, $"P3D Reading Error: Packet TryParseId error. Packet Data: {data}.");
            }
            else
                Logger.Log(LogType.Error, $"P3D Reading Error: Packet Data is null or empty.");
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
            if (Module.ClientPasswordIsCorrect(this, passwordHash))
            {
                Initialize();
                return true;
            }

            return false;
        }
        public override bool ChangePassword(string oldPassword, string newPassword)
        {
            if (PasswordHash == new PasswordStorage(oldPassword).Hash)
            {
                PasswordHash = new PasswordStorage(newPassword).Hash;
                Module.ClientUpdate(this, true);

                return true;
            }

            return false;
        }

        public override void SendPacket(Packet packet)
        {
            var p3dPacket = packet as P3DPacket;
            if(p3dPacket == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            Stream.SendPacket(packet);

#if DEBUG
            Sended.Add(p3dPacket);
#endif
        }
        public override void SendChatMessage(ChatMessage chatMessage) { SendPacket(new ChatMessageGlobalPacket { Origin = chatMessage.Sender.Id, Message = chatMessage.Message }); }
        public override void SendServerMessage(string text) { SendPacket(new ChatMessageGlobalPacket { Origin = -1, Message = text }); }
        public override void SendPrivateMessage(ChatMessage chatMessage) { SendPacket(new ChatMessagePrivatePacket { Origin = chatMessage.Sender.Id, Message = chatMessage.Message }); }

        public override void Kick(string reason = "")
        {
            SendPacket(new KickedPacket { Origin = -1, Reason = reason });
            base.Kick(reason);
        }
        public override void Ban(string reason = "")
        {
            SendPacket(new KickedPacket { Origin = -1, Reason = reason });
            base.Ban(reason);
        }


        public override void LoadFromDB(ClientTable data)
        {
            if (Id == 0)
                Id = data.Id;

            Prefix = data.Prefix;
        }


        private void Initialize()
        {
            if (!IsInitialized)
            {
                Permissions = PermissionFlags.Verified;

                Join();
                IsInitialized = true;
            }
        }

        private DataItems GenerateDataItems()
        {
            return new DataItems(
                GameMode,
                IsGameJoltPlayer ? "1" : "0",
                GameJoltId.ToString(CultureInfo),
                DecimalSeparator.ToString(),
                Name,
                LevelFile,
                Position.ToPokeString(DecimalSeparator, CultureInfo),
                Facing.ToString(CultureInfo),
                Moving ? "1" : "0",
                Skin,
                BusyType,
                PokemonVisible ? "1" : "0",
                PokemonPosition.ToPokeString(DecimalSeparator, CultureInfo),
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