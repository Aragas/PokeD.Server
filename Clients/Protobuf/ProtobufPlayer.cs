using System;
using System.Collections.Generic;
using System.Diagnostics;

using Newtonsoft.Json;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;
using PokeD.Core.Wrappers;

using PokeD.Server.Exceptions;
using PokeD.Server.IO;

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
        public long GameJoltId { get; private set; }
        [JsonIgnore]
        private char DecimalSeparator { get; set; }
        [JsonIgnore]
        public string Name { get; private set; }

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

        [JsonIgnore]
        public bool Initialized { get; private set; }

        [JsonIgnore]
        public string IP => Client.IP;

        [JsonIgnore]
        public DateTime ConnectionTime { get; } = DateTime.Now;

        [JsonIgnore]
        public DateTime LastMessage { get; private set; }
        [JsonIgnore]
        public DateTime LastPing { get; private set; }

        #endregion Other Values

        INetworkTCPClient Client { get; }
        IPacketStream Stream { get; }


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


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            if (!Stream.Connected)
                _server.RemovePlayer(this);

            if (Stream.Connected && Stream.DataAvailable > 0)
            {
                var dataLength = Stream.ReadVarInt();
                if (dataLength == 0)
                    throw new ProtobufPlayerException("Reading error: Packet Length size is 0");

                var id = Stream.ReadVarInt();
                var data = Stream.ReadByteArray(dataLength);

                LastMessage = DateTime.UtcNow;
                HandleData(id, data);
            }



            // Stuff that is done every 1 second
            if (UpdateWatch.ElapsedMilliseconds < 1000)
                return;

            if (_server.CustomWorldEnabled && UseCustomWorld)
            {
                CustomWorld.Update();
                SendPacket(new WorldDataPacket { DataItems = CustomWorld.GenerateDataItems() }, -1);
            }

            BattleUpdate();

            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        private void HandleData(int id, byte[] data)
        {
            if (data == null)
                return;

            using (var reader = new ProtobufDataReader(data))
            {
                var packet = PlayerResponse.Packets[id]().ReadPacket(reader);


                HandlePacket(packet);
#if DEBUG
                Received.Add(packet);
#endif
            }
        }

        private void HandlePacket(ProtobufPacket packet)
        {
            switch ((PlayerPacketTypes) packet.ID)
            {
                case PlayerPacketTypes.Unknown:
                    break;

                case PlayerPacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;

                case PlayerPacketTypes.PrivateMessage:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;
                case PlayerPacketTypes.ChatMessage:
                    HandleChatMessage((ChatMessagePacket) packet);
                    break;

                case PlayerPacketTypes.Ping:
                    LastPing = DateTime.UtcNow;
                    break;

                case PlayerPacketTypes.GameStateMessage:
                    HandleGameStateMessage((GameStateMessagePacket) packet);
                    break;


                case PlayerPacketTypes.TradeRequest:
                    HandleTradeRequest((TradeRequestPacket) packet);
                    break;
                case PlayerPacketTypes.TradeJoin:
                    HandleTradeJoin((TradeJoinPacket) packet);
                    break;
                case PlayerPacketTypes.TradeQuit:
                    HandleTradeQuit((TradeQuitPacket) packet);
                    break;
                case PlayerPacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;
                case PlayerPacketTypes.TradeStart:
                    HandleTradeStart((TradeStartPacket) packet);
                    break;


                case PlayerPacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;
                case PlayerPacketTypes.BattleJoin:
                    HandleBattleJoin((BattleJoinPacket) packet);
                    break;
                case PlayerPacketTypes.BattleQuit:
                    HandleBattleQuit((BattleQuitPacket) packet);
                    break;
                case PlayerPacketTypes.BattleOffer:
                    HandleBattleOffer((BattleOfferPacket) packet);
                    break;
                case PlayerPacketTypes.BattleStart:
                    HandleBattleStart((BattleStartPacket) packet);
                    break;

                case PlayerPacketTypes.BattleClientData:
                    HandleBattleClientData((BattleClientDataPacket) packet);
                    break;
                case PlayerPacketTypes.BattleHostData:
                    HandleBattleHostData((BattleHostDataPacket) packet);
                    break;
                case PlayerPacketTypes.BattlePokemonData:
                    HandleBattlePokemonData((BattlePokemonDataPacket) packet);
                    break;


                case PlayerPacketTypes.ServerDataRequest:
                    HandleServerDataRequest((ServerDataRequestPacket) packet);
                    break;
            }
        }


        public void SendPacket(ProtobufPacket packet, int originID)
        {
            if (Stream.Connected)
            {
                Stream.SendPacket(ref packet);

#if DEBUG
                Sended.Add(packet);
#endif
            }
        }
        public void SendPacket(P3DPacket packet, int originID)
        {
            throw new NotImplementedException();
        }


        public GameDataPacket GetDataPacket()
        {
            var packet = new GameDataPacket
            {
                GameMode = GameMode,
                IsGameJoltPlayer = IsGameJoltPlayer,
                GameJoltId = GameJoltId,
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


        public void Dispose()
        {
            Stream?.Dispose();

            _server.RemovePlayer(this);
        }
    }
}
