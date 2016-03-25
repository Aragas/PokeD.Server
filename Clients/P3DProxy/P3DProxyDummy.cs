using System;
using System.Globalization;

using Aragas.Core.Data;
using Aragas.Core.Packets;

using PokeD.Core.Data.P3D;
using PokeD.Core.Extensions;
using PokeD.Core.Packets.P3D.Shared;

using PokeD.Server.Data;
using PokeD.Server.Database;

namespace PokeD.Server.Clients.P3DProxy
{
    public class P3DProxyDummy : Client
    {
        CultureInfo CultureInfo => CultureInfo.InvariantCulture;

        #region P3D Values

        public override int ID { get; set; }

        public string GameMode { get; private set; }
        public bool IsGameJoltPlayer { get; private set; }
        public long GameJoltID { get; private set; }
        private char DecimalSeparator { get; set; }


        private string _name;
        public override string Name { get { return Prefix != Prefix.NONE ? $"[{Prefix}] {_name}" : _name; } protected set { _name = value; } }


        public override string LevelFile { get; protected set; }
        public override Vector3 Position { get; protected set; }
        public int Facing { get; private set; }
        public bool Moving { get; private set; }

        public string Skin { get; private set; }
        public string BusyType { get; private set; }

        public bool PokemonVisible { get; private set; }
        public Vector3 PokemonPosition { get; private set; }
        public string PokemonSkin { get; private set; }
        public int PokemonFacing { get; private set; }

        #endregion P3D Values

        #region Values

        public int SID { get; private set; }

        public override Prefix Prefix { get; protected set; }
        public override string PasswordHash { get; set; }

        public override string IP { get; }

        public override DateTime ConnectionTime { get; } = DateTime.Now;
        public override CultureInfo Language { get; }

        bool IsInitialized { get; set; }

        #endregion Values


        public P3DProxyDummy(int id, GameDataPacket packet)
        {
            SID = id;
            ParseGameData(packet);
        }
        public void ParseGameData(GameDataPacket packet)
        {
            if (packet.DataItems != null)
            {
                var strArray = packet.DataItems.ToArray();
                if (strArray.Length >= 14)
                {
                    for (var index = 0; index < strArray.Length; index++)
                    {
                        var dataItem = strArray[index];

                        if (string.IsNullOrEmpty(dataItem))
                            continue;

                        switch (index)
                        {
                            case 0:
                                GameMode = packet.GameMode;
                                break;

                            case 1:
                                IsGameJoltPlayer = packet.IsGameJoltPlayer;
                                break;

                            case 2:
                                GameJoltID = packet.GameJoltID;
                                break;

                            case 3:
                                DecimalSeparator = packet.DecimalSeparator;
                                break;

                            case 4:
                                Name = packet.Name;
                                break;

                            case 5:
                                LevelFile = packet.LevelFile;
                                break;

                            case 6:
                                Position = packet.GetPosition(DecimalSeparator);
                                break;

                            case 7:
                                Facing = packet.Facing;
                                break;

                            case 8:
                                Moving = packet.Moving;
                                break;

                            case 9:
                                Skin = packet.Skin;
                                break;

                            case 10:
                                BusyType = packet.BusyType;
                                break;

                            case 11:
                                PokemonVisible = packet.PokemonVisible;
                                break;

                            case 12:
                                if (packet.GetPokemonPosition(DecimalSeparator) != Vector3.Zero)
                                    PokemonPosition = packet.GetPokemonPosition(DecimalSeparator);
                                break;

                            case 13:
                                PokemonSkin = packet.PokemonSkin;
                                break;

                            case 14:
                                PokemonFacing = packet.PokemonFacing;
                                break;
                        }
                    }
                }
                else
                    Logger.Log(LogType.Error, $"P3D Reading Error: ParseGameData DataItems < 14. Packet DataItems {packet.DataItems}.");
            }
            else
                Logger.Log(LogType.Error, $"P3D Reading Error: ParseGameData DataItems is null.");
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
        public override GameDataPacket GetDataPacket() => new GameDataPacket { DataItems = GenerateDataItems() };

        public override void SendPacket(Packet packet) { }
        public override void SendMessage(string text)
        {
            throw new NotImplementedException();
        }

        public override void LoadFromDB(Player data) { }

        public override void Update() { }
        public override void Dispose() { }
    }
}
