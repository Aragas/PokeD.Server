/*
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Aragas.Network.Data;
using Aragas.Network.IO;
using Aragas.Network.Packets;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Prng;

using PokeD.Core.Extensions;
using PokeD.Core.IO;
using PokeD.Core.Packets;
using PokeD.Core.Packets.P3D.Battle;
using PokeD.Core.Packets.P3D.Chat;
using PokeD.Core.Packets.P3D.Client;
using PokeD.Core.Packets.P3D.Encryption;
using PokeD.Core.Packets.P3D.Server;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.P3D.Trade;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.Protobuf
{
    public partial class ProtobufPlayer : Client
    {

        #region Game Values

        [JsonIgnore]
        public int ID { get; set; }

        [JsonIgnore]
        public string GameMode { get; private set; }
        [JsonIgnore]
        public bool IsGameJoltPlayer { get; private set; }
        [JsonIgnore]
        public long GameJoltID { get; private set; }
        [JsonIgnore]
        private char DecimalSeparator { get; set; }
        private string _name;
        [JsonIgnore]
        public string Name { get { return Prefix != Prefix.NONE ? $"[{Prefix}] {_name}" : _name; } private set { _name = value; } }

        [JsonIgnore]
        public string LevelFile { get; private set; }
        [JsonIgnore]
        public Vector3 Position { get; private set; }
        [JsonIgnore]
        public int Facing { get; private set; }
        [JsonIgnore]
        public bool Moving { get; private set; }
        [JsonIgnore]
        public string Skin { get; private set; }
        [JsonIgnore]
        public string BusyType { get; private set; }
        [JsonIgnore]
        public bool PokemonVisible { get; private set; }
        [JsonIgnore]
        public Vector3 PokemonPosition { get; private set; }
        [JsonIgnore]
        public string PokemonSkin { get; private set; }
        [JsonIgnore]
        public int PokemonFacing { get; private set; }

        #endregion Game Values

        #region Other Values

        [JsonProperty("Prefix")]
        public Prefix Prefix { get; private set; }

        [JsonIgnore]
        public bool EncryptionEnabled => false;// _server.EncryptionEnabled;

        [JsonIgnore]
        public string IP => ClientWrapper.IP;

        [JsonIgnore]
        public DateTime ConnectionTime { get; } = DateTime.Now;

        [JsonIgnore]
        public DateTime LastMessage { get; private set; }
        [JsonIgnore]
        public DateTime LastPing { get; private set; }

        [JsonProperty("ChatReceiving")]
        public bool ChatReceiving => true;

        bool IsInitialized { get; set; }
        bool IsDisposed { get; set; }


        #endregion Other Values

        ITCPClient ClientWrapper { get; }
        ProtobufStream Stream { get; }


        readonly Server _server;

#if DEBUG
        // -- Debug -- //
        List<ProtobufPacket> Received { get; } = new List<ProtobufPacket>();
        List<ProtobufPacket> Sended { get; } = new List<ProtobufPacket>();
        // -- Debug -- //
#endif

        public ProtobufPlayer(ITCPClient clientWrapper, Server server)
        {
            ClientWrapper = clientWrapper;
            Stream = new ProtobufOriginStream(ClientWrapper);
            _server = server;
        }


        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public void Update()
        {
            if (Stream.Connected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var dataLength = Stream.ReadVarInt();
                    if (dataLength == 0)
                    {
                        Logger.Log(LogType.GlobalError,
                            $"Protobuf Reading Error: Packet Length size is 0. Disconnecting Client {Name}.");
                        //_server.RemovePlayer(this, "Packet Length size is 0!");
                        return;
                    }

                    var data = Stream.ReadByteArray(dataLength);

                    LastMessage = DateTime.UtcNow;
                    HandleData(data);
                }



                // Stuff that is done every 1 second
                if (UpdateWatch.ElapsedMilliseconds < 1000)
                    return;

                BattleUpdate();

                if (!Battling && _server.CustomWorldEnabled && UseCustomWorld)
                {
                    CustomWorld.Update();
                    SendPacket(new WorldDataPacket {DataItems = CustomWorld.GenerateDataItems()}, -1);
                }

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
            else
                ; //_server.RemovePlayer(this);
        }

        private void HandleData(byte[] data)
        {
            if (data != null)
            {
                using (PacketDataReader reader = new ProtobufDataReader(data))
                {
                    var id = reader.Read<VarInt>();
                    var origin = reader.Read<VarInt>();

                    if (P3DPacketResponses.Packets.Length > id)
                    {
                        if (P3DPacketResponses.Packets[id] != null)
                        {
                            var packet = P3DPacketResponses.Packets[id]().ReadPacket(reader) as ProtobufOriginPacket;
                            packet.Origin = origin;

                            HandlePacket(packet);

#if DEBUG
                            Received.Add(packet);
#endif
                        }
                        else
                        {
                            Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: GamePacketResponses.Packets[{id}] is null. Disconnecting Client {Name}.");
                            //_server.RemovePlayer(this, $"Packet ID {id} is not correct!");
                        }
                    }
                    else
                    {
                        Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: Packet ID {id} is not correct, Packet Data: {data}. Disconnecting Client {Name}.");
                        //_server.RemovePlayer(this, $"Packet ID {id} is not correct!");
                    }
                }
            }
            else
                Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: Packet Data is null.");
        }
        private void HandlePacket(ProtobufPacket packet)
        {
            switch ((P3DPacketTypes) (int) packet.ID)
            {
                case P3DPacketTypes.JoiningGameRequest:
                    HandleJoiningGameRequest((JoiningGameRequestPacket) packet);
                    break;


                case P3DPacketTypes.EncryptionResponse:
                    HandleEncryptionResponse((EncryptionResponsePacket) packet);
                    break;


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
                    LastPing = DateTime.UtcNow;
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

        private void SendEncryptionRequest()
        {
            var publicKey = _server.RSAKeyPair.PublicKeyToByteArray();

            VerificationToken = new byte[4];
            var drg = new DigestRandomGenerator(new Sha512Digest());
            drg.NextBytes(VerificationToken);

            SendPacket(new EncryptionRequestPacket { PublicKey = publicKey, VerificationToken = VerificationToken }, -1);
        }


        private void SendPacket(ProtobufOriginPacket packet, int originID)
        {
            packet.Origin = originID;
            var proto = packet as ProtobufPacket;
            if (proto == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            Stream.SendPacket(ref proto);

#if DEBUG
            Sended.Add(packet);
#endif
        }

        public void SendPacket(ProtobufPacket packet, int originID)
        {
            SendPacket(packet as ProtobufOriginPacket, originID);
        }

        public void LoadFromDB(Player data)
        {
            if (ID == 0)
                ID = data.Id;

            Prefix = data.Prefix;

            UseCustomWorld = data.IsUsingCustomWorld;
        }


        public GameDataPacket GetDataPacket()
        {
            var packet = new GameDataPacket
            {
                GameMode = GameMode,
                IsGameJoltPlayer = IsGameJoltPlayer,
                GameJoltID = GameJoltID,
                DecimalSeparator = DecimalSeparator,
                Name = Name,
                LevelFile = LevelFile,
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
            packet.SetPosition(Position, DecimalSeparator);
            packet.SetPokemonPosition(PokemonPosition, DecimalSeparator);

            return packet;
        }


        private void DisconnectAndDispose()
        {
            Stream.Disconnect();
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
*/