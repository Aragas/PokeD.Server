using System;
using System.Collections.Generic;
using System.Globalization;

using Aragas.Core.Data;
using Aragas.Core.Wrappers;

using PokeD.Core.Data;
using PokeD.Core.Extensions;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Battle;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;

using PokeD.Server.Data;

namespace PokeD.Server.Clients.NPC
{
    public partial class NPCPlayer : INPC, IClient
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region Game Values

        public int ID { get; set; }

        public string GameMode => "NPC";
        public bool IsGameJoltPlayer => false;
        public ulong GameJoltID => 0;
        private char DecimalSeparator { get; set; }
        public string Name { get; private set; } = string.Empty;

        public string LevelFile { get; private set; } = string.Empty;
        public Vector3 Position { get; private set; }
        public int Facing { get; private set; }
        public bool Moving { get; private set; }
        public string Skin { get; private set; } = string.Empty;
        public string BusyType { get; private set; } = string.Empty;
        public bool PokemonVisible { get; private set; }
        public Vector3 PokemonPosition { get; private set; }
        public string PokemonSkin { get; private set; } = string.Empty;
        public int PokemonFacing { get; private set; }

        #endregion Game Values

        #region Other Values

        public string IP => string.Empty;

        public DateTime ConnectionTime { get; } = DateTime.MinValue;

        public bool UseCustomWorld => false;

        public bool ChatReceiving => true;

        bool IsInitialized { get; set; }
        bool IsDisposed { get; set; }

        #endregion Other Values


        readonly ILua _lua;
        readonly Server _server;


#if DEBUG
        List<P3DPacket> Received { get; } =  new List<P3DPacket>();
#endif


        public NPCPlayer(string name, ILua lua, Server server)
        {
            Name = name;
            _lua = lua;
            _lua["NPC"] = this;
            _lua.ReloadFile();

            _server = server;
        }


        public void Update()
        {
            _lua.CallFunction("OnUpdate");
        }
        public void BattleUpdate(BattleDataTable battleData)
        {
            _lua.CallFunction("OnBattleUpdate", battleData);
        }


        public void SendPacket(P3DPacket packet, int originID)
        {
#if DEBUG
            Received.Add(packet);
#endif

            HandlePacket(packet);
        }
        private void HandlePacket(P3DPacket packet)
        {
            switch ((GamePacketTypes) (int) packet.ID)
            {
                case GamePacketTypes.ChatMessagePrivate:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case GamePacketTypes.ChatMessageGlobal:
                    HandleChatMessage((ChatMessageGlobalPacket) packet);
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
        
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;


        }


        public void Move(int x, int y, int z)
        {

        }

        public void SayPlayerPM(int playerID, string message)
        {
            _server.SendPrivateChatMessageToClient(playerID, message, ID);
        }
        public void SayGlobal(string message)
        {
            _server.SendGlobalChatMessageToAllClients(message);
        }
    }
}