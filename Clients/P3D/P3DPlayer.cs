using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using Newtonsoft.Json;

using PokeD.Core.Data;
using PokeD.Core.Extensions;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Server;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;

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
        P3DStream Stream { get; }


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


        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public void Update()
        {
            if (Stream.Connected)
            {
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
                    SendPacket(new WorldDataPacket {DataItems = CustomWorld.GenerateDataItems()}, -1);
                }

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
            else
                _server.RemovePlayer(this);
        }

        private void HandleData(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                int id;
                if (P3DPacket.TryParseID(data, out id))
                {
                    if (GamePacketResponses.Packets.Length > id)
                    {
                        if (GamePacketResponses.Packets[id] != null)
                        {
                            var packet = GamePacketResponses.Packets[id]();
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
                            Logger.Log(LogType.GlobalError, $"P3D Reading Error: SCONPacketResponses.Packets[{id}] is null.");
                    }
                    else
                        Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packets Length {GamePacketResponses.Packets.Length} > Packet ID {id}, Packet Data: {data}.");
                }
                else
                    Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet TryParseID error. Packet Data: {data}.");
            }
            else
                Logger.Log(LogType.GlobalError, $"P3D Reading Error: Packet Data is null or empty.");
        }
        private void HandlePacket(P3DPacket packet)
        {
            switch ((GamePacketTypes) packet.ID)
            {
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
        public GameDataPacket GetDataPacket()
        {
            return new GameDataPacket { DataItems = GenerateDataItems() };
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