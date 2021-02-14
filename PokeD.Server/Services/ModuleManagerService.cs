using Aragas.TupleEventSystem;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD;
using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;
using PokeD.Server.Storage.Folders;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.Services
{
    public sealed class ClientJoinedEventArgs : EventArgs
    {
        public Client Client { get; }

        public ClientJoinedEventArgs(Client client) { Client = client; }
    }
    public sealed class ClientLeavedEventArgs : EventArgs
    {
        public Client Client { get; }

        public ClientLeavedEventArgs(Client client) { Client = client; }
    }

    public sealed class ModuleManagerService : IHostedService, IDisposable
    {
        private List<ServerModule> Modules { get; } = new();

        public BaseEventHandler<ClientJoinedEventArgs> ClientJoined = new WeakReferenceEventHandler<ClientJoinedEventArgs>();
        public BaseEventHandler<ClientLeavedEventArgs> ClientLeaved = new WeakReferenceEventHandler<ClientLeavedEventArgs>();


        //public BaseEventHandler<ServerSentMessageEventArgs> ServerSentMessage = new CustomEventHandler<ServerSentMessageEventArgs>();

        private CancellationTokenSource UpdateToken { get; set; } = new();
        private ManualResetEventSlim UpdateLock { get; } = new(false);

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DatabaseService _databaseService;

        public ModuleManagerService(ILogger<ModuleManagerService> logger, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            //_logger = serviceProvider.GetRequiredService<ILogger<ModuleManagerService>>();
            _databaseService = serviceProvider.GetRequiredService<DatabaseService>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            //_databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        private Assembly AppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            _logger.LogDebug($"Resolving assembly {args.Name}");
            foreach (var moduleFile in new ModulesFolder().GetModuleFiles())
            {
                _logger.LogDebug($"Searching assembly in module {moduleFile.Name}");
                var assembly = moduleFile.GetAssembly(args.Name);
                if (assembly != null)
                {
                    _logger.LogDebug("Found assembly!");
                    return assembly;
                }
            }

            _logger.LogDebug($"Assembly {args.Name} not found!");
            return null;
        }

        public IReadOnlyList<IServerModuleBaseSettings> GetModuleSettings() => Modules;

        public void AllClientsForeach(Action<IReadOnlyList<Client>> func)
        {
            foreach (var module in Modules.Where(module => module.ClientsVisible))
                module.ClientsForeach(func);
        }
        public TResult AllClientsSelect<TResult>(Func<IReadOnlyList<Client>, TResult> func) => Modules.Where(module => module.ClientsVisible).Select(module => module.ClientsSelect(func)).FirstOrDefault();
        public IReadOnlyList<TResult> AllClientsSelect<TResult>(Func<IReadOnlyList<Client>, IReadOnlyList<TResult>> func) => Modules.Where(module => module.ClientsVisible).SelectMany(module => module.ClientsSelect(func)).ToList();

        public Client GetClient(int id) => AllClientsSelect(list => list.Where(client => client.ID == id).ToList()).FirstOrDefault();
        public Client GetClient(string name) => AllClientsSelect(list => list.Where(client => client.Name == name || client.Nickname == name).ToList()).FirstOrDefault();
        public int GetClientID(string name) => GetClient(name)?.ID ?? -1;
        public string GetClientName(int id) => GetClient(id)?.Nickname ?? string.Empty;

        public void SendServerMessage(string message)
        {
            foreach (var module in Modules)
                module.OnServerMessage(message);

            _logger.Log(LogLevel.Information, new EventId(10, "Chat"), message);
        }

        #region Trade

        private List<TradeInstance> CurrentTrades { get; } = new();

        public void TradeRequest(Client sender, DataItems monster, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => !Equals(serverModule, callerModule)))
                module.OnTradeRequest(sender, monster, destClient);


            TradeInstance trade;
            if (!CurrentTrades.Any(t => t.Equals(sender.ID, destClient.ID)))
                CurrentTrades.Add(trade = new TradeInstance { Client0ID = sender.ID, Client1ID = destClient.ID });
            else
                trade = CurrentTrades.First(t => t.Equals(sender.ID, destClient.ID));
            try // TODO: specify exceptions that should be processed here.
            {
                if (trade.Client0ID == sender.ID)
                    trade.Client0Monster = new Monster(monster);

                if (trade.Client1ID == sender.ID)
                    trade.Client1Monster = new Monster(monster);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while creating trade request! Type: {e.GetType()}, Message: {e.Message}");
                CurrentTrades.Remove(trade);
            }


            _logger.Log(LogLevel.Information, new EventId(20, "Trade"), $"{sender.Name} sent a trade request to {destClient.Name}. Module {callerModule.GetType().Name}");
        }
        public void TradeConfirm(Client sender, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => !Equals(serverModule, callerModule)))
                module.OnTradeConfirm(sender, destClient);


            var trade = CurrentTrades.Find(t => t.Equals(sender.ID, destClient.ID));
            if (trade == null)
            {
                _logger.LogError("Error while confirming trade request! trade was null.");
                return;
            }
            try
            {
                if (trade.Client0ID == sender.ID)
                    trade.Client0Confirmed = true;
                if (trade.Client1ID == sender.ID)
                    trade.Client1Confirmed = true;

                if (trade.Client0Confirmed && trade.Client1Confirmed)
                {
                    _databaseService.DatabaseSet(new TradeTable(_databaseService, trade));
                    CurrentTrades.Remove(trade);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while confirming trade request! Type: {e.GetType()}, Message: {e.Message}");
                CurrentTrades.Remove(trade);
            }


            _logger.Log(LogLevel.Information, new EventId(20, "Trade"), $"{sender.Name} confirmed a trade request with {destClient.Name}. Module {callerModule.GetType().Name}");
        }
        public void TradeCancel(Client sender, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => !Equals(serverModule, callerModule)))
                module.OnTradeCancel(sender, destClient);


            var trade = CurrentTrades.Find(t => t.Equals(sender.ID, destClient.ID));
            if (trade == null)
            {
                _logger.LogError($"Error while cancelling trade request! trade was null.");
                return;
            }
            CurrentTrades.Remove(trade);


            _logger.Log(LogLevel.Information, new EventId(20, "Trade"), $"{sender.Name} cancelled a trade request with {destClient.Name}. Module {callerModule.GetType().Name}");
        }

        #endregion

        public void Kick(Client client, string reason = "")
        {
            client.SendKick(reason);
            _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"Player {client.Name} was kicked!");
        }
        public void Ban(Client client, int minutes = 0, string reason = "")
        {
            var previousBan = _databaseService.DatabaseGet<BanTable>(client.ID);
            if (previousBan != null)
            {
                _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"Player {client.Name} was already banned! Reason - \"{previousBan.Reason}\". Unban time - {previousBan.UnbanTime:G}!");
                return;
            }

            var banTable = new BanTable(client, DateTime.UtcNow + TimeSpan.FromMinutes(minutes <= 0 ? int.MaxValue : minutes), reason);
            _databaseService.DatabaseSet(banTable);
            client.SendBan(banTable);
            _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"Player {client.Name} was banned. Reason - \"{banTable.Reason}\". Unban time - {banTable.UnbanTime:G}!");
        }
        public void Unban(Client client)
        {
            _databaseService.DatabaseRemove<BanTable>(client.ID);
            _logger.Log(LogLevel.Information, new EventId(30, "Event"), $"Player {client.Name} was unbanned!");
        }

        public (bool IsBanned, BanTable BanTable) BanStatus(Client client)
        {
            var banTable = _databaseService.DatabaseGet<BanTable>(client.ID);
            if (banTable != null)
            {
                if (banTable.UnbanTime - DateTime.UtcNow < TimeSpan.Zero)
                {
                    _databaseService.DatabaseRemove<BanTable>(client.ID);
                    return (false, null);
                }
                else
                    return (true, banTable);
            }
            else
                return (false, null);
        }


        public static long UpdateThread { get; private set; }
        private void UpdateCycle()
        {
            UpdateLock.Reset();

            var watch = Stopwatch.StartNew();
            while (!UpdateToken.IsCancellationRequested)
            {
                foreach (var module in Modules)
                    module.Update();

                if (watch.ElapsedMilliseconds < 10)
                {
                    UpdateThread = watch.ElapsedMilliseconds;

                    var time = (int) (10 - watch.ElapsedMilliseconds);
                    if (time < 0) time = 0;
                    Thread.Sleep(time);
                }
                watch.Reset();
                watch.Start();
            }

            UpdateLock.Set();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Modules.Clear();
            //Modules.Add(new ModuleSCON(_serviceProvider));
            Modules.Add(new ModuleP3D(_serviceProvider));

            _logger.LogDebug("Starting Modules...");
            foreach (var module in Modules)
                await module.StartAsync(cancellationToken);
            _logger.LogDebug("Started Modules.");

            _logger.LogDebug("Starting UpdateThread...");
            UpdateToken = new CancellationTokenSource();
            new Thread(UpdateCycle)
            {
                Name = "ModuleManagerUpdateTread",
                IsBackground = true
            }.Start();
            _logger.LogDebug("Started UpdateThread.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stopping UpdateThread...");
            if (UpdateToken?.IsCancellationRequested == false)
            {
                UpdateToken.Cancel();
                UpdateLock.Wait(cancellationToken);
            }
            _logger.LogDebug("Stopped UpdateThread.");

            _logger.LogDebug("Stopping Modules...");
            foreach (var module in Modules)
                await module.StopAsync(cancellationToken);
            _logger.LogDebug("Stopped Modules.");
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AppDomain_AssemblyResolve;

            ClientJoined?.Dispose();
            ClientLeaved?.Dispose();

            if (UpdateToken?.IsCancellationRequested == false)
            {
                UpdateToken.Cancel();
                UpdateLock.Wait();
            }
        }
    }
}