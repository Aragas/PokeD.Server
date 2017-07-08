using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Aragas.Network.Data;
using Aragas.Network.Extensions;
using Aragas.Network.Packets;

using PCLExt.Config;
using PCLExt.FileStorage;
using PCLExt.FileStorage.Extensions;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD;
using PokeD.Core.Packets.PokeD.Authorization;
using PokeD.Core.Packets.PokeD.Chat;
using PokeD.Core.Packets.PokeD.Overworld.Map;
using PokeD.Core.Packets.PokeD.Overworld;
using PokeD.Core.Packets.PokeD.Trade;
using PokeD.Core.Services;
using PokeD.Core.Storage.Folders;
using PokeD.Server.Clients;
using PokeD.Server.Clients.PokeD;
using PokeD.Server.Services;
using PokeD.Server.Storage.Files;

/*
using TMXParserPCL;
*/

namespace PokeD.Server.Modules
{
    public class ModulePokeD : ServerModule
    {
        protected override string ComponentName { get; } = "ModulePokeD";
        protected override IConfigFile ComponentConfigFile => new ModulePokeDConfigFile(ConfigType);

        static IFolder Maps => new DataFolder().CreateFolder("Maps", CreationCollisionOption.OpenIfExists);
        static IFolder TileSets => Maps.CreateFolder("TileSets", CreationCollisionOption.OpenIfExists);

        #region Settings

        public override bool Enabled { get; protected set; } = false;

        public override ushort Port { get; protected set; } = 15130;

        public bool EncryptionEnabled { get; protected set; } = true;
        
        #endregion Settings

        bool IsDisposing { get; set; }

        TcpListener Listener { get; set; }

        List<PokeDPlayer> Clients { get; } = new List<PokeDPlayer>();
        List<PokeDPlayer> PlayersJoining { get; } = new List<PokeDPlayer>();
        List<PokeDPlayer> PlayersToAdd { get; } = new List<PokeDPlayer>();
        List<PokeDPlayer> PlayersToRemove { get; } = new List<PokeDPlayer>();


        public ModulePokeD(IServiceContainer services, ConfigType configType) : base(services, configType) { }


        public override bool Start()
        {
            if (!base.Start())
                return false;


            Logger.Log(LogType.Debug, $"Starting {ComponentName}.");

            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            Listener.Start();


            ModuleManager.ClientJoined += (this, ModuleManager_ClientJoined);
            ModuleManager.ClientLeaved += (this, ModuleManager_ClientLeaved);


            return true;
        }
        public override bool Stop()
        {
            if (!base.Stop())
                return false;


            Logger.Log(LogType.Debug, $"Stopping {ComponentName}.");

            ModuleManager.ClientJoined -= ModuleManager_ClientJoined;
            ModuleManager.ClientLeaved -= ModuleManager_ClientLeaved;

            Dispose();


            return true;
        }

        private void ModuleManager_ClientJoined(object sender, ClientJoinedEventArgs eventArgs)
        {
            //PokeDPlayerSendToAllClients(new CreatePlayerPacket { PlayerID = client.ID }, -1);
            //PokeDPlayerSendToAllClients(client.GetDataPacket(), client.ID);
            SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {eventArgs.Client.Name} joined the game!" });
        }
        private void ModuleManager_ClientLeaved(object sender, ClientLeavedEventArgs eventArgs)
        {
            //PokeDPlayerSendToAllClients(new DestroyPlayerPacket { PlayerID = client.ID });
            SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {eventArgs.Client.Name} disconnected!" });
        }


        /*
        public override IReadOnlyList<Client> GetClients() => Clients;
        */
        
        public override void ClientsForeach(Action<IReadOnlyList<Client>> action)
        {
            lock (Clients)
                action(Clients);
        }
        public override TResult ClientsSelect<TResult>(Func<IReadOnlyList<Client>, TResult> func)
        {
            lock (Clients)
                return func(Clients);
        }
        public override IReadOnlyList<TResult> ClientsSelect<TResult>(Func<IReadOnlyList<Client>, IReadOnlyList<TResult>> func)
        {
            lock (Clients)
                return func(Clients);
        }

        protected override void OnClientReady(object sender, EventArgs eventArgs)
        {
            var client = sender as PokeDPlayer;
            if (client == null)
                return;

            if (!AssignID(client))
            {
                client.SendKick("You are already on server!");
                return;
            }

            client.SendPacket(new AuthorizationCompletePacket { PlayerID = new VarInt(client.ID) });
            SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {client.Name} joined the game!" });

            PlayersToAdd.Add(client);
            PlayersJoining.Remove(client);

            base.OnClientReady(sender, eventArgs);
        }
        protected override void OnClientLeave(object sender, EventArgs eventArgs)
        {
            var client = sender as PokeDPlayer;
            if (client == null)
                return;

            SendPacketToAll(new ChatGlobalMessagePacket { Message = $"Player {client.Name} disconnected!" });
            PlayersToRemove.Add(client);

            base.OnClientLeave(sender, eventArgs);
        }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public override void Update()
        {
            if (Listener?.Pending() == true)
                PlayersJoining.Add(new PokeDPlayer(Listener.AcceptSocket(), this));

            #region Player Filtration

            for (var i = 0; i < PlayersToAdd.Count; i++)
            {
                var playerToAdd = PlayersToAdd[i];

                Clients.Add(playerToAdd);
                PlayersToAdd.Remove(playerToAdd);

                if (playerToAdd.ID != 0)
                {
                    /*
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
                    */
                }
            }

            for (var i = 0; i < PlayersToRemove.Count; i++)
            {
                var playerToRemove = PlayersToRemove[i];

                Clients.Remove(playerToRemove);
                PlayersJoining.Remove(playerToRemove);
                PlayersToRemove.Remove(playerToRemove);

                playerToRemove.Dispose();
            }

            #endregion Player Filtration


            #region Player Updating

            // Update actual players
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i]?.Update();

            // Update joining players
            for (var i = PlayersJoining.Count - 1; i >= 0; i--)
                PlayersJoining[i]?.Update();

            #endregion Player Updating


            if (UpdateWatch.ElapsedMilliseconds > 5000)
            {
                for (var i = Clients.Count - 1; i >= 0; i--)
                    Clients[i]?.SendPacket(new PingPacket());

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
        }

        public void SendPacketToAll(Packet packet)
        {
            for (var i = Clients.Count - 1; i >= 0; i--)
                Clients[i]?.SendPacket(packet);
        }

        public override void OnTradeRequest(Client sender, DataItems monster, Client destClient)
        {
            base.OnTradeRequest(sender, monster, destClient);

            if (destClient is PokeDPlayer)
                destClient.SendPacket(new TradeOfferPacket { DestinationID = new VarInt(-1), MonsterData = new Monster(monster) });
        }
        public override void OnTradeConfirm(Client sender, Client destClient)
        {
            base.OnTradeConfirm(sender, destClient);

            if (destClient is PokeDPlayer)
            {
                destClient.SendPacket(new TradeAcceptPacket { DestinationID = new VarInt(-1) });

                Thread.Sleep(5000);
            }
        }
        public override void OnTradeCancel(Client sender, Client destClient)
        {
            base.OnTradeCancel(sender, destClient);

            if (destClient is PokeDPlayer)
                destClient.SendPacket(new TradeRefusePacket{ DestinationID = new VarInt(-1) });
        }

        public override void OnPosition(Client sender)
        {
            base.OnPosition(sender);

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

                var image = new ImageResponse { Name = tileSetName };
                using (var fileStream = TileSets.GetFileAsync($"{tileSetName}.png").Result.OpenAsync(PCLExt.FileStorage.FileAccess.Read).Result)
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

            for (var i = Clients.Count - 1; i >= 0; i--)
            {
                Clients[i].SendKick("Closing server!");
                Clients[i].Dispose();
            }
            Clients.Clear();

            for (var i = PlayersToAdd.Count - 1; i >= 0; i--)
            {
                PlayersToAdd[i].SendKick("Closing server!");
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