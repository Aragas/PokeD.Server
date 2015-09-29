using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

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

using PokeD.Server.IO;

namespace PokeD.Server.Clients.P3D
{
    public partial class P3DPlayer : IClient
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

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

        [JsonProperty("ChatReceiving")]
        public bool ChatReceiving => true;

        #endregion Other Values

        INetworkTCPClient Client { get; }
        IPacketStream Stream { get; }


        readonly Server _server;

#if DEBUG
        // -- Debug -- //
        List<P3DPacket> Received { get; } =  new List<P3DPacket>();
        List<P3DPacket> Sended { get; } = new List<P3DPacket>();
        // -- Debug -- //
#endif


        public P3DPlayer(INetworkTCPClient client, Server server)
        {
            Client = client;
            Stream = new P3DStream(Client);
            _server = server;
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {
            if (!Stream.Connected)
            {
                _server.RemovePlayer(this);
                return;
            }

            if (Stream.Connected && Stream.DataAvailable > 0)
            {
                var data = Stream.ReadLine();

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
                SendPacket(new WorldDataPacket { DataItems = CustomWorld.GenerateDataItems() }, -1);
            }
            
            UpdateWatch.Reset();
            UpdateWatch.Start();
        }


        private void HandleData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet Data is null or empty.");
                return;
            }


            int id;
            if (P3DPacket.TryParseID(data, out id))
            {
                if (id >= PlayerResponse.Packets.Length)
                {
                    Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet ID {id} is not correct, Packet Data: {data}.");
                    return;
                }

                var packet = PlayerResponse.Packets[id]();
                if (packet == null)
                {
                    Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet is null. Packet ID {id}, Packet Data: {data}.");
                    return;
                }

                //packet = PlayerResponse.Packets[id]();
                if (packet.TryParseData(data))
                {
                    HandlePacket(packet);
#if DEBUG
                    Received.Add(packet);
#endif
                }
                else
                    Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet TryParseData error. Packet ID {id}, Packet Data: {data}.");
            }
            else
                Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet TryParseID error. Packet Data: {data}.");
        }
        private void HandlePacket(P3DPacket packet)
        {
            switch ((PlayerPacketTypes) packet.ID)
            {
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
            throw new NotImplementedException();
        }
        public void SendPacket(P3DPacket packet, int originID)
        {
            if (Stream.Connected)
            {
                packet.Origin = originID;

                Stream.SendPacket(ref packet);

#if DEBUG
                Sended.Add(packet);
#endif
            }
        }


        public GameDataPacket GetDataPacket()
        {
            return new GameDataPacket { DataItems = GenerateDataItems() };
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


        public void Disconnect()
        {
            Stream.Disconnect();
        }


        public void Dispose()
        {
            Stream?.Dispose();

            _server.RemovePlayer(this);
        }
    }
}