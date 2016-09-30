/*
using System.Diagnostics;

using PCLExt.Config;

using PokeD.Core.Data.PokeD.Monster;
using PokeD.Server.Clients;
using PokeD.Server.Clients.Pixelmon;
using PokeD.Server.Extensions;

namespace PokeD.Server
{
    public class ModulePixelmon : IServerModule
    {
        const string FileName = "ModulePixelmon";

        #region Settings

        public bool Enabled { get; private set; } = true;

        public string IP { get; private set; } = "127.0.0.1";

        public ushort Port { get; private set; } = 25565;

        public string Login { get; private set; } = "micvitalij@yandex.ru";

        public string Password { get; private set; } = "753951";

        #endregion Settings

        [ConfigIgnore]
        public Server Server { get; }
        bool IsDisposing { get; set; }

        [ConfigIgnore]
        public ClientList Clients { get; } = new ClientList();
        [ConfigIgnore]
        public bool ClientsVisible { get; } = false;

        PixelmonClient Client { get; set; }


        public ModulePixelmon(Server server) { Server = server; }


        public bool Start()
        {
            var status = FileSystemExtensions.LoadSettings(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to load Pixelmon settings!");

            if (!Enabled)
            {
                Logger.Log(LogType.Info, $"Pixelmon not enabled!");
                return false;
            }

            Logger.Log(LogType.Info, $"Starting Pixelmon.");


            Client = new PixelmonClient(Login, Password);


            return true;
        }
        public void Stop()
        {
            var status = FileSystemExtensions.SaveSettings(Server.ConfigType, FileName, this);
            if (!status)
                Logger.Log(LogType.Warning, "Failed to save Pixelmon settings!");

            Logger.Log(LogType.Info, $"Stopping Pixelmon.");

            Dispose();

            Logger.Log(LogType.Info, $"Stopped Pixelmon.");
        }


        public void StartListen()
        {
            var statusClient = Client.CreateStatusClient();
            var serverInfo = statusClient.GetServerInfo(IP, Port);

            Client.Connect(serverInfo);
            Client.ConnectToServer(serverInfo.Address.IP, serverInfo.Address.Port, Client.Username);
        }
        public void CheckListener() { }


        Stopwatch UpdateWatch = Stopwatch.StartNew();
        public void Update()
        {

        }


        public void OtherConnected(Client client)
        {
            //Message = $"Player {client.Name} joined the game!";
        }
        public void OtherDisconnected(Client client)
        {
            //Message = $"Player {client.Name} disconnected!";
        }

        public void SendServerMessage(Client sender, string message)
        {
            //if (sender is PokeDPlayer)
            //{
            //    PokeDPlayerSendToAllClients(new ChatServerMessagePacket() { Message = message });
            //    Server.ClientServerMessage(this, sender, message);
            //}
            //else
            //    PokeDPlayerSendToAllClients(new ChatServerMessagePacket() { Message = message });
        }
        public void SendPrivateMessage(Client sender, Client destClient, string message)
        {
            //if (destClient is PokeDPlayer)
            //    PokeDPlayerSendToClient(destClient, new ChatPrivateMessagePacket() { Message = message });
            //else
            //    Server.ClientPrivateMessage(this, sender, destClient, message);
        }
        public void SendGlobalMessage(Client sender, string message)
        {
            //if (sender is PokeDPlayer)
            //{
            //    PokeDPlayerSendToAllClients(new ChatGlobalMessagePacket() { Message = message });
            //
            //    Server.ClientGlobalMessage(this, sender, message);
            //}
            //else
            //    PokeDPlayerSendToAllClients(new ChatGlobalMessagePacket() { Message = message });
        }

        public void SendTradeRequest(Client sender, Monster monster, Client destClient)
        {
            //if (destClient is PokeDPlayer)
            //    PokeDPlayerSendToClient(destClient, new TradeOfferPacket() { DestinationID = new VarInt(-1), MonsterData = monster.InstanceData });
            //else
            //    Server.ClientTradeOffer(this, sender, monster, destClient);
        }
        public void SendTradeConfirm(Client sender, Client destClient)
        {
            //if (destClient is PokeDPlayer)
            //{
            //    PokeDPlayerSendToClient(destClient, new TradeAcceptPacket() { DestinationID = new VarInt(-1) });
            //
            //    Server.ClientTradeConfirm(this, sender, destClient);
            //
            //    Thread.Sleep(5000);
            //}
            //else
            //    Server.ClientTradeConfirm(this, sender, destClient);
        }
        public void SendTradeCancel(Client sender, Client destClient)
        {
            //if (destClient is PokeDPlayer)
            //    PokeDPlayerSendToClient(destClient, new TradeRefusePacket() { DestinationID = new VarInt(-1) });
            //else
            //    Server.ClientTradeCancel(this, sender, destClient);
        }

        public void SendPosition(Client sender)
        {
            //if (sender is PokeDPlayer)
            //    Server.ClientPosition(this, sender);
            //else
            //{
            //    var posData = sender.GetDataPacket();
            //    PokeDPlayerSendToAllClients(new PositionPacket() { Position = posData.GetPosition(posData.DecimalSeparator) });
            //}
        }


        public void Dispose()
        {
            if (IsDisposing)
                return;

            IsDisposing = true;


        }
    }
}
*/