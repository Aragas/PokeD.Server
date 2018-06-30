using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;

using Aragas.Network.Data;
using Aragas.Network.IO;
using Aragas.Network.Packets;

using PokeD.Core.Data;
using PokeD.Core.Data.PokeD;
using PokeD.Core.Packets.PokeD;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.PokeD.Authorization;
using PokeD.Core.Packets.PokeD.Battle;
using PokeD.Core.Packets.PokeD.Chat;
using PokeD.Core.Packets.PokeD.Overworld.Map;
using PokeD.Core.Packets.PokeD.Overworld;
using PokeD.Core.Packets.PokeD.Trade;
using PokeD.Server.Chat;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;

namespace PokeD.Server.Clients.PokeD
{
    public partial class PokeDPlayer : Client<ModulePokeD>
    {
        Trainer PlayerRef { get; set; } = new Trainer(1);

        #region Game Values

        public override int ID 
        { 
            get => PlayerRef.ID;
            set 
            { 
                // PlayerRef.ID = new VarInt(value);
            } 
        }

        public string GameMode => "PokeD Game";
        public bool IsGameJoltPlayer => true;
        public long GameJoltID => 0;

        private char DecimalSeparator => '.';

        public override string Nickname 
        { 
            get => PlayerRef.Name;
            protected set 
            { 
                throw new NotSupportedException(); 
                // PlayerRef.Name = value;
            }
        }

        public override string LevelFile { get; set; }

        public override Vector3 Position
        {
            get
            {
                return Vector3.Zero;
                //return PlayerRef.Position;
            }
            set => throw new NotSupportedException();
        }

        public int Facing => 0; //PlayerRef.Facing;
        public bool Moving => true;
        public string Skin => string.Empty; //PlayerRef.TrainerSprite.ToString();
        public string BusyType { get; private set; }
        public bool PokemonVisible => false;
        public Vector3 PokemonPosition => Vector3.Zero;
        public string PokemonSkin => string.Empty;
        public int PokemonFacing => 0;

        #endregion Game Values

        #region Other Values

        public override Prefix Prefix { get; protected set; }
        public override string PasswordHash { get; set; }

        public override string IP => Stream.Host;

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language { get; }
        public override PermissionFlags Permissions { get; set; }

        bool IsInitialized { get; set; }

        #endregion Other Values

        private BasePacketFactory<PokeDPacket, VarInt, ProtobufSerializer, ProtobufDeserialiser> PacketFactory { get; }
        private ProtobufTransmission<PokeDPacket> Stream { get; }

#if DEBUG
        // -- Debug -- //
        private const int QueueSize = 1000;
        private Queue<PokeDPacket> Received { get; } = new Queue<PokeDPacket>(QueueSize);
        private Queue<PokeDPacket> Sended { get; } = new Queue<PokeDPacket>(QueueSize);
        // -- Debug -- //
#endif

        private bool IsDisposing { get; set; }

        public PokeDPlayer(Socket socket, ModulePokeD module) : base(module)
        {
            PacketFactory = new PacketEnumFactory<PokeDPacket, PokeDPacketTypes, VarInt, ProtobufSerializer, ProtobufDeserialiser>();
            Stream = new ProtobufTransmission<PokeDPacket>(socket, factory: PacketFactory);
        }


        public override void Update()
        {
            if (Stream.IsConnected)
            {
                PokeDPacket packet;
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

        private void HandlePacket(PokeDPacket packet)
        {
            switch ((PokeDPacketTypes) (int) packet.ID)
            {
                case PokeDPacketTypes.AuthorizationRequest:
                    HandleAuthorizationRequest((AuthorizationRequestPacket) packet);
                    break;
                case PokeDPacketTypes.EncryptionResponse:
                    HandleEncryptionResponse((EncryptionResponsePacket) packet);
                    break;


                case PokeDPacketTypes.Position:
                    HandlePosition((PositionPacket) packet);
                    break;
                case PokeDPacketTypes.TrainerInfo:
                    HandleTrainerInfo((TrainerInfoPacket) packet);
                    break;

                case PokeDPacketTypes.TileSetRequest:
                    HandleTileSetRequest((TileSetRequestPacket) packet);
                    break;


                case PokeDPacketTypes.ChatServerMessage:
                    HandleChatServerMessage((ChatServerMessagePacket) packet);
                    break;
                case PokeDPacketTypes.ChatGlobalMessage:
                    HandleChatGlobalMessage((ChatGlobalMessagePacket) packet);
                    break;
                case PokeDPacketTypes.ChatPrivateMessage:
                    HandleChatPrivateMessage((ChatPrivateMessagePacket) packet);
                    break;


                case PokeDPacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;
                case PokeDPacketTypes.BattleAccept:
                    HandleBattleAccept((BattleAcceptPacket) packet);
                    break;

                case PokeDPacketTypes.BattleAttack:
                    HandleBattleAttack((BattleAttackPacket) packet);
                    break;
                case PokeDPacketTypes.BattleItem:
                    HandleBattleItem((BattleItemPacket) packet);
                    break;
                case PokeDPacketTypes.BattleSwitch:
                    HandleBattleSwitch((BattleSwitchPacket) packet);
                    break;
                case PokeDPacketTypes.BattleFlee:
                    HandleBattleFlee((BattleFleePacket) packet);
                    break;


                case PokeDPacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;
                case PokeDPacketTypes.TradeAccept:
                    HandleTradeAccept((TradeAcceptPacket) packet);
                    break;
                case PokeDPacketTypes.TradeRefuse:
                    HandleTradeRefuse((TradeRefusePacket) packet);
                    break;
            }
        }


        public override bool RegisterOrLogIn(string passwordHash) => false;
        public override bool ChangePassword(string oldPassword, string newPassword) => false;

        public override void SendPacket<TPacket>(TPacket packet)
        {
            if (!(packet is PokeDPacket pokeDPacket))
                throw new Exception($"Wrong packet type, {typeof(TPacket).FullName}");

            Stream.SendPacket(pokeDPacket);

#if DEBUG
            Sended.Enqueue(pokeDPacket);
            if (Sended.Count >= QueueSize)
                Sended.Dequeue();
#endif
        }
        public override void SendChatMessage(ChatChannel chatChannel, ChatMessage chatMessage) { SendPacket(new ChatGlobalMessagePacket { Message = chatMessage.Message }); }
        public override void SendServerMessage(string text) { SendPacket(new ChatServerMessagePacket { Message = text }); }
        public override void SendPrivateMessage(ChatMessage chatMessage) { SendPacket(new ChatPrivateMessagePacket { PlayerID = new VarInt(chatMessage.Sender.ID), Message = chatMessage.Message }); }

        public override void SendKick(string reason = "")
        {
            SendPacket(new DisconnectPacket { Reason = reason });
            base.SendKick(reason);
        }
        public override void SendBan(BanTable banTable)
        {
            SendPacket(new DisconnectPacket { Reason = $"You have banned from this server\r\nReason: {banTable.Reason}\r\nTime left: {(banTable.UnbanTime - DateTime.UtcNow):%m} minutes." });
            base.SendBan(banTable);
        }


        public override void Load(ClientTable data)
        {
            base.Load(data);

            LevelFile = data.LevelFile;
            Prefix = data.Prefix;
        }

        public override GameDataPacket GetDataPacket()
        {
            var packet = new GameDataPacket
            {
                GameMode = GameMode,
                IsGameJoltPlayer = IsGameJoltPlayer,
                GameJoltID = GameJoltID,
                DecimalSeparator = DecimalSeparator,
                Name = Name,
                LevelFile = ToP3DLevelFile(),
                //Position = Position,
                Facing = Facing,
                Moving = Moving,
                Skin = Skin,
                BusyType = BusyType,
                PokemonVisible = PokemonVisible,
                //PokemonPosition = PokemonPosition,
                PokemonSkin = PokemonSkin,
                PokemonFacing = PokemonFacing
            };

            var posOffset = Vector3.Zero;
            switch (LevelFile)
            {
                case "0.0.tmx":
                    posOffset = new Vector3(+3.0f, 0.0f, -3.0f);
                    break;
            }

            packet.SetPosition(Position + posOffset, DecimalSeparator);
            packet.SetPokemonPosition(PokemonPosition + posOffset, DecimalSeparator);

            return packet;
        }
        
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


        private string ToP3DLevelFile()
        {
            if(string.IsNullOrEmpty(LevelFile))
                return "barktown.dat";

            switch (LevelFile)
            {
                case "0.0.tmx":
                    return "barktown.dat";
            }
            return "mainmenu";
        }
    }
}