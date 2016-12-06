using System;
using System.Collections.Generic;
using System.Globalization;

using Aragas.Network.Data;
using Aragas.Network.IO;
using Aragas.Network.Packets;

using PCLExt.Network;

using PokeD.Core.Data.PokeD.Trainer;
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

namespace PokeD.Server.Clients.PokeD
{
    public partial class PokeDPlayer : Client<ModulePokeD>
    {
        Trainer PlayerRef { get; set; } = new Trainer("1112");

        #region Game Values

        public override int Id { get { return PlayerRef.EntityId; } set { PlayerRef.EntityId = new VarInt(value); } }

        public string GameMode => "PokeD Game";
        public bool IsGameJoltPlayer => true;
        public long GameJoltId => 0;

        private char DecimalSeparator => '.';

        public override string Nickname { get { return PlayerRef.Name; } protected set { PlayerRef.Name = value; } }

        public override string LevelFile { get; set; }
        public override Vector3 Position { get { return PlayerRef.Position; } set { throw  new NotSupportedException(); } }

        public int Facing => PlayerRef.Facing;
        public bool Moving => true;
        public string Skin => PlayerRef.TrainerSprite.ToString();
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

        ProtobufStream Stream { get; }

#if DEBUG
        // -- Debug -- //
        List<ProtobufPacket> Received { get; } = new List<ProtobufPacket>();
        List<ProtobufPacket> Sended { get; } = new List<ProtobufPacket>();
        // -- Debug -- //
#endif

        public PokeDPlayer(ITCPClient clientWrapper, ModulePokeD module) : base(module) { Stream = new ProtobufStream(clientWrapper); }


        public override void Update()
        {
            if (Stream.IsConnected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var dataLength = Stream.ReadVarInt();
                    if (dataLength == 0)
                    {
                        Logger.Log(LogType.Error, $"PokeD Reading Error: Packet Length size is 0. Disconnecting IClient {Name}.");
                        Kick("Packet Length size is 0!");
                        return;
                    }

                    var data = Stream.Receive(dataLength);

                    HandleData(data);
                }
            }
            else
                Kick();
        }

        private void HandleData(byte[] data)
        {
            if (data != null)
            {
                using (var reader = new ProtobufDataReader(data))
                {
                    var id = reader.Read<VarInt>();

                    Func<PokeDPacket> func;
                    if (PokeDPacketResponses.TryGetPacketFunc(id, out func))
                    {
                        if (func != null)
                        {
                            var packet = func().ReadPacket(reader);

                            HandlePacket(packet);

#if DEBUG
                            Received.Add(packet);
#endif
                        }
                        else
                        {
                            Logger.Log(LogType.Error, $"PokeD Reading Error: PokeDPacketResponses.Packets[{id}] is null. Disconnecting IClient {Name}.");
                            Kick($"Packet Id {id} is not correct!");
                        }
                    }
                    else
                    {
                        Logger.Log(LogType.Error, $"PokeD Reading Error: Packet Id {id} is not correct, Packet Data: {data}. Disconnecting IClient {Name}.");
                        Kick($"Packet Id {id} is not correct!");
                    }
                }
            }
            else
                Logger.Log(LogType.Error, $"PokeD Reading Error: Packet Data is null.");
        }
        private void HandlePacket(ProtobufPacket packet)
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

        public override void SendPacket(Packet packet)
        {
            var pokeDPacket = packet as PokeDPacket;
            if (pokeDPacket == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            Stream.SendPacket(packet);

#if DEBUG
            Sended.Add(pokeDPacket);
#endif
        }
        public override void SendChatMessage(ChatMessage chatMessage) { SendPacket(new ChatGlobalMessagePacket { Message = chatMessage.Message }); }
        public override void SendServerMessage(string text) { SendPacket(new ChatServerMessagePacket { Message = text }); }
        public override void SendPrivateMessage(ChatMessage chatMessage) { SendPacket(new ChatPrivateMessagePacket { PlayerId = new VarInt(chatMessage.Sender.Id), Message = chatMessage.Message }); }

        public override void Kick(string reason = "")
        {
            SendPacket(new DisconnectPacket { Reason = reason });

            base.Kick(reason);
        }
        public override void Ban(string reason = "")
        {
            SendPacket(new DisconnectPacket { Reason = reason });

            base.Ban(reason);
        }


        public override void LoadFromDB(ClientTable data)
        {
            if (Id == 0)
                Id = data.Id;

            LevelFile = data.LevelFile;
            Prefix = data.Prefix;
        }

        public override GameDataPacket GetDataPacket()
        {
            var packet = new GameDataPacket
            {
                GameMode = GameMode,
                IsGameJoltPlayer = IsGameJoltPlayer,
                GameJoltId = GameJoltId,
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
        
        public override void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
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