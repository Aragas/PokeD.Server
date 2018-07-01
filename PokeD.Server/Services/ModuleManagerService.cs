using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.P3D;
using PokeD.Core.Data.PokeD;
using PokeD.Core.Event;
using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;
using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Services
{
    public class ClientJoinedEventArgs : EventArgs
    {
        public Client Client { get; }

        public ClientJoinedEventArgs(Client client) { Client = client; }
    }
    public class ClientLeavedEventArgs : EventArgs
    {
        public Client Client { get; }

        public ClientLeavedEventArgs(Client client) { Client = client; }
    }

    public class ModuleManagerService : BaseServerService
    {
        private List<ServerModule> Modules { get; } = new List<ServerModule>();


        public BaseEventHandler<ClientJoinedEventArgs> ClientJoined = new WeakReferenceEventHandler<ClientJoinedEventArgs>();
        public BaseEventHandler<ClientLeavedEventArgs> ClientLeaved = new WeakReferenceEventHandler<ClientLeavedEventArgs>();
        

        //public BaseEventHandler<ServerSentMessageEventArgs> ServerSentMessage = new CustomEventHandler<ServerSentMessageEventArgs>();

        private CancellationTokenSource UpdateToken { get; set; } = new CancellationTokenSource();
        private ManualResetEventSlim UpdateLock { get; } = new ManualResetEventSlim(false);

        private bool IsDisposed { get; set; }

        public ModuleManagerService(IServiceContainer services, ConfigType configType) : base(services, configType)
        {
            Modules.Add(new ModuleSCON(Services, ConfigType));
            //Modules.Add(new ModuleNPC(this, ConfigType));
            Modules.Add(new ModuleP3D(Services, ConfigType));
            //Modules.Add(new ModulePokeD(this, ConfigType));

            //foreach (var module in LoadModules())
            //    Modules.Add(module);
        }    
        private IEnumerable<ServerModule> LoadModules()
        {
            Logger.Log(LogType.Debug, "Loading external modules");
            AppDomain.CurrentDomain.AssemblyResolve += AppDomain_AssemblyResolve;

            foreach (var moduleFile in new ModulesFolder().GetModuleFiles())
            {
                Logger.Log(LogType.Debug, $"Loading module {moduleFile.Name}");
                var assembly = moduleFile.GetModule();

                var serverModule = assembly?.ExportedTypes.SingleOrDefault(type => type.GetTypeInfo().IsSubclassOf(typeof(ServerModule)) && !type.GetTypeInfo().IsAbstract);
                if (serverModule != null)
                {
                    Logger.Log(LogType.Debug, $"Created module {serverModule.FullName}");
                    yield return (ServerModule) Activator.CreateInstance(serverModule, Services, ConfigType);
                }
            }
        }
        private Assembly AppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Logger.Log(LogType.Debug, $"Resolving assembly {args.Name}");
            foreach (var moduleFile in new ModulesFolder().GetModuleFiles())
            {
                Logger.Log(LogType.Debug, $"Searching assembly in module {moduleFile.Name}");
                var assembly = moduleFile.GetAssembly(args.Name);
                if (assembly != null)
                {
                    Logger.Log(LogType.Debug, "Found assembly!");
                    return assembly;
                }
            }
            
            Logger.Log(LogType.Debug, $"Assembly {args.Name} not found!");
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

            Logger.Log(LogType.Chat, message);
        }

        #region Trade
        
        private List<TradeInstance> CurrentTrades { get; } = new List<TradeInstance>();

        public void TradeRequest(Client sender, DataItems monster, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => !Equals(serverModule, callerModule)))
                module.OnTradeRequest(sender, monster, destClient);


            TradeInstance trade;
            if (!CurrentTrades.Any(t => t.Equals(sender.ID, destClient.ID)))
                CurrentTrades.Add((trade = new TradeInstance { Client0ID = sender.ID, Client1ID = destClient.ID }));
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
                Logger.Log(LogType.Error, $"Error while creating trade request! Type: {e.GetType()}, Message: {e.Message}");
                CurrentTrades.Remove(trade);
            }


            Logger.Log(LogType.Trade, $"{sender.Name} sent a trade request to {destClient.Name}. Module {callerModule.GetType().Name}");
        }
        public void TradeConfirm(Client sender, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => !Equals(serverModule, callerModule)))
                module.OnTradeConfirm(sender, destClient);


            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(sender.ID, destClient.ID));
            if (trade == null)
            {
                Logger.Log(LogType.Error, "Error while confirming trade request! trade was null.");
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
                    Services.GetService<DatabaseService>().DatabaseSet(new TradeTable(Services.GetService<DatabaseService>(), trade));
                    CurrentTrades.Remove(trade);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Error while confirming trade request! Type: {e.GetType()}, Message: {e.Message}");
                CurrentTrades.Remove(trade);
            }


            Logger.Log(LogType.Trade, $"{sender.Name} confirmed a trade request with {destClient.Name}. Module {callerModule.GetType().Name}");
        }
        public void TradeCancel(Client sender, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => !Equals(serverModule, callerModule)))
                module.OnTradeCancel(sender, destClient);


            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(sender.ID, destClient.ID));
            if (trade == null)
            {
                Logger.Log(LogType.Error, $"Error while cancelling trade request! trade was null.");
                return;
            }
            CurrentTrades.Remove(trade);


            Logger.Log(LogType.Trade, $"{sender.Name} cancelled a trade request with {destClient.Name}. Module {callerModule.GetType().Name}");
        }

        #endregion

        public void Kick(Client client, string reason = "")
        {
            client.SendKick(reason);
            Logger.Log(LogType.Event, $"Player {client.Name} was kicked!");
        }
        public void Ban(Client client, int minutes = 0, string reason = "")
        {
            var previousBan = Services.GetService<DatabaseService>().DatabaseGet<BanTable>(client.ID);
            if (previousBan != null)
            {
                Logger.Log(LogType.Event, $"Player {client.Name} was already banned! Reason - \"{previousBan.Reason}\". Unban time - {previousBan.UnbanTime:G}!");
                return;
            }

            var banTable = new BanTable(client, DateTime.UtcNow + TimeSpan.FromMinutes(minutes <= 0 ? int.MaxValue : minutes), reason);
            Services.GetService<DatabaseService>().DatabaseSet(banTable);
            client.SendBan(banTable);
            Logger.Log(LogType.Event, $"Player {client.Name} was banned. Reason - \"{banTable.Reason}\". Unban time - {banTable.UnbanTime:G}!");
        }
        public void Unban(Client client)
        {
            Services.GetService<DatabaseService>().DatabaseRemove<BanTable>(client.ID);
            Logger.Log(LogType.Event, $"Player {client.Name} was unbanned!");
        }

        public (bool IsBanned, BanTable BanTable) BanStatus(Client client)
        {
            var banTable = Services.GetService<DatabaseService>().DatabaseGet<BanTable>(client.ID);
            if (banTable != null)
            {
                if (banTable.UnbanTime - DateTime.UtcNow < TimeSpan.Zero)
                {
                    Services.GetService<DatabaseService>().DatabaseRemove<BanTable>(client.ID);
                    return (false, null);
                }
                else
                    return (true, banTable);
            }
            else
                return (false, null);
        }

        public override bool Start()
        {
            Logger.Log(LogType.Debug, "Starting Modules...");
            Modules.RemoveAll(module => !module.Start());
            Logger.Log(LogType.Debug, "Started Modules.");

            Logger.Log(LogType.Debug, "Starting UpdateThread...");
            UpdateToken = new CancellationTokenSource();
            new Thread(UpdateCycle)
            {
                Name = "ModuleManagerUpdateTread",
                IsBackground = true
            }.Start();
            Logger.Log(LogType.Debug, "Started UpdateThread.");

            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, "Stopping UpdateThread...");
            if (UpdateToken?.IsCancellationRequested == false)
            {
                UpdateToken.Cancel();
                UpdateLock.Wait();
            }
            Logger.Log(LogType.Debug, "Stopped UpdateThread.");

            Logger.Log(LogType.Debug, "Stopping Modules...");
            foreach (var module in Modules)
                module.Stop();
            Logger.Log(LogType.Debug, "Stopped Modules.");

            return true;
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

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
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


                IsDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}