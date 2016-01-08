using System;
using System.Collections.Generic;

using Aragas.Core.Data;
using Aragas.Core.IO;
using Aragas.Core.Packets;
using Aragas.Core.Wrappers;

using PokeD.Core.Data.PokeD.Trainer;
using PokeD.Core.Data.PokeD.Trainer.Data;
using PokeD.Core.Packets;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Core.Packets.PokeD.Battle;
using PokeD.Core.Packets.PokeD.Overworld;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.PokeD
{
    public partial class PokeDPlayer : IClient
    {
        Trainer PlayerRef { get; } = new Trainer("1123123", TrainerGender.Female);

        #region Game Values

        public int ID { get { return PlayerRef.EntityID; } set { PlayerRef.EntityID = value; } }

        public string GameMode => "PokeD Game";
        public bool IsGameJoltPlayer => true;
        public long GameJoltID => 0;

        private char DecimalSeparator => '.';

        public string Name { get { return Prefix != Prefix.NONE ? $"[{Prefix}] {PlayerRef.Name}" : PlayerRef.Name; } private set { PlayerRef.Name = value; } }

        public string LevelFile => ToP3DLevelFile(PlayerRef.Location);
        public Vector3 Position => PlayerRef.Position;
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

        public Prefix Prefix { get; private set; }
        public string PasswordHash { get; set; }

        //public string IP => Stream.Host; // TODO
        public string IP => "";

        public DateTime ConnectionTime { get; } = DateTime.Now;

        public DateTime LastMessage { get; private set; }

        bool EncryptionEnabled => Module.EncryptionEnabled;

        bool IsInitialized { get; set; }
        
        #endregion Other Values

        ProtobufStream Stream { get; }
        
        ModulePokeD Module { get; }

#if DEBUG
        // -- Debug -- //
        List<PokeDPacket> Received { get; } = new List<PokeDPacket>();
        List<PokeDPacket> Sended { get; } = new List<PokeDPacket>();
        // -- Debug -- //
#endif

        public PokeDPlayer(ITCPClient clientWrapper, IServerModule modulo)
        {
            Stream = new ProtobufStream(clientWrapper);
            Module = (ModulePokeD) modulo;
        }


        public void Update()
        {
            if (Stream.Connected)
            {
                if (Stream.DataAvailable > 0)
                {
                    var dataLength = Stream.ReadVarInt();
                    if (dataLength == 0)
                    {
                        Logger.Log(LogType.GlobalError, $"PokeD Reading Error: Packet Length size is 0. Disconnecting IClient {Name}.");
                        Module.RemoveClient(this, "Packet Length size is 0!");
                        return;
                    }

                    var data = Stream.ReadByteArray(dataLength);

                    LastMessage = DateTime.UtcNow;
                    HandleData(data);
                }
            }
            else
                Module.RemoveClient(this);
        }

        private void HandleData(byte[] data)
        {
            if (data != null)
            {
                using (PacketDataReader reader = new ProtobufDataReader(data))
                {
                    var id = reader.Read<VarInt>();

                    if (PokeDPacketResponses.Packets.Length > id)
                    {
                        if (PokeDPacketResponses.Packets[id] != null)
                        {
                            var packet = PokeDPacketResponses.Packets[id]().ReadPacket(reader) as PokeDPacket;

                            HandlePacket(packet);

#if DEBUG
                            Received.Add(packet);
#endif
                        }
                        else
                        {
                            Logger.Log(LogType.GlobalError, $"PokeD Reading Error: PokeDPacketResponses.Packets[{id}] is null. Disconnecting IClient {Name}.");
                            Module.RemoveClient(this, $"Packet ID {id} is not correct!");
                        }
                    }
                    else
                    {
                        Logger.Log(LogType.GlobalError, $"PokeD Reading Error: Packet ID {id} is not correct, Packet Data: {data}. Disconnecting IClient {Name}.");
                        Module.RemoveClient(this, $"Packet ID {id} is not correct!");
                    }
                }
            }
            else
                Logger.Log(LogType.GlobalError, $"PokeD Reading Error: Packet Data is null.");
        }
        private void HandlePacket(PokeDPacket packet)
        {
            switch ((PokeDPacketTypes) (int) packet.ID)
            {
                case PokeDPacketTypes.Position:
                    HandlePosition((PositionPacket) packet);
                    break;
                case PokeDPacketTypes.TrainerInfo:
                    HandleTrainerInfo((TrainerInfoPacket) packet);
                    break;


                case PokeDPacketTypes.BattleRequest:
                    HandleBattleRequest((BattleRequestPacket) packet);
                    break;
                case PokeDPacketTypes.BattleAccept:
                    HandleBattleAccept((BattleAcceptPacket)packet);
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
            }
        }

        public void SendPacket(ProtobufPacket packet, int originID = 0)
        {
            var pokeDPacket = packet as PokeDPacket;
            if (pokeDPacket == null)
                throw new Exception($"Wrong packet type, {packet.GetType().FullName}");

            Stream.SendPacket(ref packet);

#if DEBUG
            Sended.Add(pokeDPacket);
#endif
        }

        public void LoadFromDB(Player data)
        {
            if (ID == 0)
                ID = data.Id;

            Prefix = data.Prefix;
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
        
        public void Dispose()
        {
            Stream.Disconnect();
            Stream.Dispose();
        }


        public static string ToP3DLevelFile(string location)
        {
            return "mainmenu";
        }
    }
}
