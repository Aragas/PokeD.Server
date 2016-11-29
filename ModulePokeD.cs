using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Aragas.Network.Data;
using Aragas.Network.Extensions;
using Aragas.Network.Packets;

using PCLExt.Config.Extensions;
using PCLExt.Network;
using PCLExt.Thread;
using PCLExt.FileStorage;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Core.Packets.PokeD.Authorization;
using PokeD.Core.Packets.PokeD.Chat;
using PokeD.Core.Packets.PokeD.Overworld.Map;
using PokeD.Core.Packets.PokeD.Overworld;
using PokeD.Core.Packets.PokeD.Trade;
using PokeD.Server.Clients;
using PokeD.Server.Clients.PokeD;

using TMXParserPCL;

namespace PokeD.Server
{
    public class ModulePokeD : ServerModule
    {
        const string FileName = "ModulePokeD";

        static IFolder Maps => Storage.ContentFolder.CreateFolderAsync("Maps", CreationCollisionOption.OpenIfExists).Result;
        static IFolder TileSets => Maps.CreateFolderAsync("TileSets", CreationCollisionOption.OpenIfExists).Result;

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15130;

        public bool EncryptionEnabled { get; protected set; } = true;
        
        #endregion Settings

        bool IsDisposing { get; set; }

        ITCPListener Listener { get; set; }
        
        List<Client> PlayersJoining { get; } = new List<Client>();
        List<Client> PlayersToAdd { get; } = new List<Client>();
        List<Client> PlayersToRemove { get; } = new List<Client>();


        public ModulePokeD(Server server) : base(server) { }


        public override bool Start()
        {
            var status = FileSystemExtensions.LoadConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load PokeD settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"PokeD not enabled!");
                return false;
            }

            Logger.Log(LogType.Info, $"Starting PokeD.");

            return true;
        }
        public override void Stop()
        {
            var status = FileSystemExtensions.SaveConfig(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save PokeD settings!");

            Logger.Log(LogType.Info, $"Stopping PokeD.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped PokeD.");
        }


        public override void StartListen()
        {
            Listener = SocketServer.CreateTCP(Port);
            Listener.Start();
        }
        public override void CheckListener()
        {
            if (Listener != null && Listener.AvailableClients)
                if (Listener.AvailableClients)
                    PlayersJoining.Add(new PokeDPlayer(Listener.AcceptTCPClient(), this));
        }


        public override void AddClient(Client client)
        {
            if (!Server.DatabaseSetClientId(client))
            {
                RemoveClient(client, "You are already on server!");
                return;
            }

            if (!Server.DatabasePlayerLoad(client))
            {
                RemoveClient(client, "Wrong password!");
                return;
            }

            client.SendPacket(new AuthorizationCompletePacket { PlayerID = new VarInt(client.ID) });

            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);
        }
        public override void RemoveClient(Client client, string reason = "")
        {
            client.Kick(reason);

            PlayersToRemove.Add(client);
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public override void Update()
        {
            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {playerToAdd.Name} joined the game!" });

                    Server.NotifyClientConnected(this, playerToAdd);


                    var mapData = Maps.GetFileAsync("0.0.tmx").Result.ReadAllTextAsync().Result;
                    //var mapData = Maps.GetFileAsync(playerToAdd.LevelFile).Result.ReadAllTextAsync().Result;

                    #region Hash

                    Map map;
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(mapData)))
                        map = Map.Load(stream);
                    
                    var tileSetHashesh = map.TileSets.Select(tileSet => new FileHash()
                                {
                                    Name = tileSet.Source.Replace(".tsx", ""),
                                    Hash = TileSets.GetFileAsync(tileSet.Source).Result.MD5Hash()
                                });

                    var imageHashesh = map.TileSets.Select(tileSet =>
                                new FileHash()
                                {
                                    Name = tileSet.Source.Replace(".tsx", ""),
                                    Hash = TileSets.GetFileAsync(tileSet.Source.Replace(".tsx", ".png")).Result.MD5Hash()
                                });
                    #endregion Hash


                    playerToAdd.SendPacket(new MapPacket()
                    {
                        MapData = mapData,
                        TileSetHashes = tileSetHashesh.ToArray(),
                        ImageHashes = imageHashesh.ToArray()
                    });
                }
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Clients.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

                if (playerToRemove.ID != 0)
                {
                    SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {playerToRemove.Name} disconnected!" });
                    
                    Server.NotifyClientDisconnected(this, playerToRemove);
                }

                playerToRemove.Dispose();
            }

            #endregion Player Filtration


            #region Player Updating

            // Update actual players
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.Update();

            // Update joining players
            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i]?.Update();

            #endregion Player Updating


            if (UpdateWatch.ElapsedMilliseconds > 5000)
            {
                for (var i = 0; i < Clients.Count; i++)
                    Clients[i]?.SendPacket(new PingPacket());

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
        }


        public override void ClientConnected(Client client)
        {
            //PokeDPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            //PokeDPlayerSendToAllClients(client.GetDataPacket(), client.ID);
            SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {client.Name} joined the game!" });
        }
        public override void ClientDisconnected(Client client)
        {
            //PokeDPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = client.ID });
            SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {client.Name} disconnected!" });
        }

        public override void SendPacketToAll(Packet packet)
        {
            for (var i = 0; i < Clients.Count; i++)
                Clients[i]?.SendPacket(packet);
        }

        public override void SendTradeRequest(Client sender, Monster monster, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeOffer(this, sender, monster, destClient);

            if (destClient is PokeDPlayer)
                destClient.SendPacket(new TradeOfferPacket { DestinationID = new VarInt(-1), MonsterData = monster.InstanceData });
        }
        public override void SendTradeConfirm(Client sender, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeConfirm(this, sender, destClient);

            if (destClient is PokeDPlayer)
            {
                destClient.SendPacket(new TradeAcceptPacket { DestinationID = new VarInt(-1) });

                Thread.Sleep(5000);
            }
        }
        public override void SendTradeCancel(Client sender, Client destClient, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientTradeCancel(this, sender, destClient);

            if (destClient is PokeDPlayer)
                destClient.SendPacket(new TradeRefusePacket{ DestinationID = new VarInt(-1) });
        }

        public override void SendPosition(Client sender, bool fromServer = false)
        {
            if (!fromServer)
                Server.NotifyClientPosition(this, sender);

            if (sender is PokeDPlayer)
                return;
            else
            {
                var posData = sender.GetDataPacket();
                SendPacketToAll(new PositionPacket() { Position = posData.GetPosition(posData.DecimalSeparator) });
            }
        }


        public void PokeDTileSetRequest(Client player, IEnumerable<string> tileSetNames)
        {
            var tileSets = new List<TileSetResponse>();
            var images = new List<ImageResponse>();


            foreach (var tileSetName in tileSetNames)
            {
                tileSets.Add(new TileSetResponse() { Name = tileSetName, TileSetData = TileSets.GetFileAsync($"{tileSetName}.tsx").Result.ReadAllTextAsync().Result });

                var image = new ImageResponse();
                image.Name = tileSetName;
                using (var fileStream = TileSets.GetFileAsync($"{tileSetName}.png").Result.OpenAsync(FileAccess.Read).Result)
                    image.ImageData = fileStream.ReadFully();
                images.Add(image);
            }

            player.SendPacket(new TileSetResponsePacket()
            {
                TileSets = tileSets.ToArray(),
                Images = images.ToArray()
            });
        }


        public override void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


            for (var i = 0; i < PlayersJoining.Count; i++)
                PlayersJoining[i].Dispose();
            PlayersJoining.Clear();

            for (var i = 0; i < Clients.Count; i++)
            {
                Clients[i].Kick("Closing server!");
                Clients[i].Dispose();
            }
            Clients.Clear();

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                PlayersToAdd[i].Kick("Closing server!");
                PlayersToAdd[i].Dispose();
            }
            PlayersToAdd.Clear();

            // Do not dispose PlayersToRemove!
            PlayersToRemove.Clear();

            //for (int i = 0; i < Battles.Count; i++)
            //    Battles[i].Dispose();
            //Battles.Clear();
        }
    }
}