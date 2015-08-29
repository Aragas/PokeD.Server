using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using PokeD.Core.Data;
using PokeD.Core.Interfaces;
using PokeD.Core.IO;
using PokeD.Core.Packets;
using PokeD.Core.Packets.Chat;
using PokeD.Core.Packets.Client;
using PokeD.Core.Packets.Shared;
using PokeD.Core.Packets.Trade;
using PokeD.Core.Wrappers;

using PokeD.Server.IO;

namespace PokeD.Server.Data
{
    public partial class Player : IUpdatable, IDisposable
    {

        private INetworkTcpClient Client { get; set; }
        private IPokeStream Stream { get; set; }

        private Task CurrentTask { get; set; } 
        private TimeSpan TaskTimeLimit { get; set; }


        private readonly Server _server;

        // -- Debug -- //
        readonly List<IPacket> _received = new List<IPacket>();
        readonly List<IPacket> _sended = new List<IPacket>();
        // -- Debug -- //

        #region Game values

        public int ID { get; set; }

        public string GameMode { get; set; }
        public bool IsGameJoltPlayer { get; set; }
        public long GameJoltId { get; set; }
        public char DecimalSeparator { get; set; }
        public string Name { get; set; }
        public string LevelFile { get; set; }
        public Vector3 Position { get; set; }
        public int Facing { get; set; }
        public bool Moving { get; set; }
        public string Skin { get; set; }
        public string BusyType { get; set; }
        public bool PokemonVisible { get; set; }
        public Vector3 PokemonPosition { get; set; }
        public string PokemonSkin { get; set; }
        public int PokemonFacing { get; set; }

        public bool Initialized { get; set; }

        public DateTime LastMessage { get; set; }
        public DateTime LastPing { get; set; }


        #endregion Game values


        public Player(INetworkTcpClient client, Server server)
        {
            Client = client;
            Stream = new PlayerStream(Client);

            TaskTimeLimit = new TimeSpan(0, 0, 0, 0, 500);

            _server = server;
        }


        public void Update()
        {
            /*
            if ((CurrentTask == null || CurrentTask.IsCompleted) && (Client.Connected && Client.DataAvailable > 0))
            {
                CurrentTask = Task.Factory.StartNew(() =>
                {
                    var data = Stream.ReadLine();

                    LastMessage = DateTime.UtcNow;
                    HandleData(Encoding.UTF8.GetBytes(data));
                });
            }
            */

            ///*
            if (Client.Connected && Client.DataAvailable > 0)
            {
                var data = Stream.ReadLine();

                LastMessage = DateTime.UtcNow;
                HandleData(Encoding.UTF8.GetBytes(data));
            }
            //*/
        }


        private void HandleData(byte[] data)
        {
            using (var reader = new StreamReader(new MemoryStream(data)))
            {
                var str = reader.ReadLine();

                if (string.IsNullOrEmpty(str))
                    return;

                var packet = Response.Packets[IPacket.ParseID(str)]();
                packet.ParseData(str);
                InputWrapper.ConsoleWrite(((PacketTypes) packet.ID).ToString());
                _received.Add(packet);


                HandlePacket(packet);

                //if (SavePackets)
                //    PacketsReceived.Add(packet);
                //
                //if (SavePacketsToDisk)
                //    DumpPacketReceived(packet, data);
            }
        }

        private void HandlePacket(IPacket packet)
        {
            switch ((PacketTypes) packet.ID)
            {
                case PacketTypes.Unknown:
                    //ServerClient.QueueMessage("Invalid Data have been received from " + ((IPEndPoint)Client.Client.RemoteEndPoint).Address.ToString(), Main.LogType.Debug, null);
                    break;

                case PacketTypes.GameData:
                    HandleGameData((GameDataPacket) packet);
                    break;

                case PacketTypes.PrivateMessage:
                    HandlePrivateMessage((ChatMessagePrivatePacket) packet);
                    break;

                case PacketTypes.ChatMessage:
                    HandleChatMessage((ChatMessagePacket) packet);
                    break;

                case PacketTypes.Ping:
                    LastPing = DateTime.UtcNow;
                    break;

                case PacketTypes.GameStateMessage:
                    HandleGameStateMessage((GameStateMessagePacket) packet);
                    break;

                case PacketTypes.TradeRequest:
                    HandleTradeRequest((TradeRequestPacket) packet);
                    break;

                case PacketTypes.TradeJoin:
                    HandleTradeJoin((TradeJoinPacket) packet);
                    break;

                case PacketTypes.TradeQuit:
                    HandleTradeQuit((TradeQuitPacket) packet);
                    break;

                case PacketTypes.TradeOffer:
                    HandleTradeOffer((TradeOfferPacket) packet);
                    break;

                case PacketTypes.TradeStart:
                    HandleTradeStart((TradeStartPacket) packet);
                    break;

                case PacketTypes.BattleRequest:
                    //HandleBattleRequest(Data, Client);
                    break;

                case PacketTypes.BattleJoin:
                    //HandleBattleJoin(Data, Client);
                    break;

                case PacketTypes.BattleQuit:
                    //HandleBattleQuit(Data, Client);
                    break;

                case PacketTypes.BattleOffer:
                    //HandleBattleOffer(Data, Client);
                    break;

                case PacketTypes.BattleStart:
                    //HandleBattleStart(Data, Client);
                    break;

                case PacketTypes.BattleClientData:
                    //HandleBattleClientData(Data, Client);
                    break;

                case PacketTypes.BattleHostData:
                    //HandleBattleHostData(Data, Client);
                    break;

                case PacketTypes.BattlePokemonData:
                    //HandleBattlePokemonData(Data, Client);
                    break;

                case PacketTypes.ServerDataRequest:
                    HandleServerDataRequest((ServerDataRequestPacket) packet);
                    break;

            }
        }

        
        public void SendPacket(IPacket packet)
        {
            if (Stream.Connected)
            {
                packet.ProtocolVersion = _server.ProtocolVersion;
                packet.Origin = ID;
                _sended.Add(packet);
                Stream.SendPacket(ref packet);
            }
        }

        public void SendPacketCustom(IPacket packet, int origin = -1)
        {
            if (Stream.Connected)
            {
                packet.ProtocolVersion = _server.ProtocolVersion;
                packet.Origin = origin;
                _sended.Add(packet);
                Stream.SendPacket(ref packet);
            }
        }




        public List<string> GeneratePlayerData()
        {
            List<string> list = new List<string>();
            list.Add(GameMode);
            list.Add(IsGameJoltPlayer ? "1" : "0");
            list.Add(GameJoltId.ToString());
            list.Add(DecimalSeparator.ToString());
            list.Add(Name);
            list.Add(LevelFile);
            list.Add(Position.ToPokeString());
            list.Add(Facing.ToString());
            list.Add(Moving ? "1" : "0");
            list.Add(Skin.ToString());
            list.Add(BusyType);
            list.Add(PokemonVisible ? "1" : "0");
            list.Add(PokemonPosition.ToPokeString());
            list.Add(PokemonSkin);
            list.Add(PokemonFacing.ToString());
            return list;
        }


        public void Dispose()
        {

        }
    }
}