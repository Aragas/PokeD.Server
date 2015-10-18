using System;
using System.Collections.Generic;
using System.Diagnostics;

using Aragas.Core.Data;
using Aragas.Core.Interfaces;
using Aragas.Core.IO;
using Aragas.Core.Packets;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Prng;

using PokeD.Core.Extensions;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Encryption;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;
using PokeD.Server.Data;

namespace PokeD.Server.Clients.Protobuf
{
    public partial class ProtobufPlayer : IClient
    {

        #region Game Values

        [JsonIgnore]
        public int ID { get; set; }

        [JsonIgnore]
        public string GameMode { get; private set; }
        [JsonIgnore]
        public bool IsGameJoltPlayer { get; private set; }
        [JsonIgnore]
        public ulong GameJoltID { get; private set; }
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
        public bool EncryptionEnabled => _server.EncryptionEnabled;

        [JsonIgnore]
        public string IP => Client.IP;

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

        INetworkTCPClient Client { get; }
        ProtobufStream Stream { get; }


        readonly Server _server;

#if DEBUG
        // -- Debug -- //
        List<ProtobufPacket> Received { get; } = new List<ProtobufPacket>();
        List<ProtobufPacket> Sended { get; } = new List<ProtobufPacket>();
        // -- Debug -- //
#endif

        public ProtobufPlayer(INetworkTCPClient client, Server server)
        {
            Client = client;
            Stream = new ProtobufStream(Client);
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
                        Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: Packet Length size is 0. Disconnecting IClient {Name}.");
                        _server.RemovePlayer(this, "Packet Length size is 0!");
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
                _server.RemovePlayer(this);
        }

        private void HandleData(byte[] data)
        {
            if (data != null)
            {
                using (IPacketDataReader reader = new ProtobufDataReader(data))
                {
                    var id = reader.Read<VarInt>();
                    var origin = reader.Read<VarInt>();

                    if (GamePacketResponses.Packets.Length > id)
                    {
                        if (GamePacketResponses.Packets[id] != null)
                        {
                            var packet = GamePacketResponses.Packets[id]().ReadPacket(reader);
                            packet.Origin = origin;

                            HandlePacket(packet);

#if DEBUG
                            Received.Add(packet);
#endif
                        }
                        else
                        {
                            Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: GamePacketResponses.Packets[{id}] is null. Disconnecting IClient {Name}.");
                            _server.RemovePlayer(this, $"Packet ID {id} is not correct!");
                        }
                    }
                    else
                    {
                        Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: Packet ID {id} is not correct, Packet Data: {data}. Disconnecting IClient {Name}.");
                        _server.RemovePlayer(this, $"Packet ID {id} is not correct!");
                    }
                }
            }
            else
                Logger.Log(LogType.GlobalError, $"Protobuf Reading Error: Packet Data is null.");
        }
        private void HandlePacket(ProtobufPacket packet)
        {
            switch ((GamePacketTypes) (int) packet.ID)
            {
                case GamePacketTypes.JoiningGameRequest:
                    HandleJoiningGameRequest((JoiningGameRequestPacket) packet);
                    break;


                case GamePacketTypes.EncryptionResponse:
                    HandleEncryptionResponse((EncryptionResponsePacket) packet);
                    break;


                case GamePacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;

                case GamePacketTypes.ChatMessagePrivate:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;
                case GamePacketTypes.ChatMessageGlobal:
                    HandleChatMessage((ChatMessageGlobalPacket) packet);
                    break;

                case GamePacketTypes.Ping:
                    LastPing = DateTime.UtcNow;
                    break;

                case GamePacketTypes.GameStateMessage:
                    HandleGameStateMessage((GameStateMessagePacket) packet);
                    break;


                case GamePacketTypes.TradeRequest:
                    HandleTradeRequest((TradeRequestPacket) packet);
                    break;
                case GamePacketTypes.TradeJoin:
                    HandleTradeJoin((TradeJoinPacket) packet);
                    break;
                case GamePacketTypes.TradeQuit:
                    HandleTradeQuit((TradeQuitPacket) packet);
                    break;
                case GamePacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;
                case GamePacketTypes.TradeStart:
                    HandleTradeStart((TradeStartPacket) packet);
                    break;


                case GamePacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;
                case GamePacketTypes.BattleJoin:
                    HandleBattleJoin((BattleJoinPacket) packet);
                    break;
                case GamePacketTypes.BattleQuit:
                    HandleBattleQuit((BattleQuitPacket) packet);
                    break;
                case GamePacketTypes.BattleOffer:
                    HandleBattleOffer((BattleOfferPacket) packet);
                    break;
                case GamePacketTypes.BattleStart:
                    HandleBattleStart((BattleStartPacket) packet);
                    break;

                case GamePacketTypes.BattleClientData:
                    HandleBattleClientData((BattleClientDataPacket) packet);
                    break;
                case GamePacketTypes.BattleHostData:
                    HandleBattleHostData((BattleHostDataPacket) packet);
                    break;
                case GamePacketTypes.BattlePokemonData:
                    HandleBattlePokemonData((BattlePokemonDataPacket) packet);
                    break;


                case GamePacketTypes.ServerDataRequest:
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


        private void SendPacket(ProtobufPacket packet, int originID)
        {
            packet.Origin = originID;

            Stream.SendPacket(ref packet);

#if DEBUG
            Sended.Add(packet);
#endif
        }

        public void SendPacket(P3DPacket packet, int originID)
        {
            SendPacket(packet as ProtobufPacket, originID);
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
