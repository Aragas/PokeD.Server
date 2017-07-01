using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Data.P3D;
using PokeD.Core.Services;
using PokeD.Server.Clients;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;
using PokeD.Server.Storage.Folders;

namespace PokeD.Server.Services
{
    public abstract class BaseEventHandler<TEventArgs> : IDisposable where TEventArgs : EventArgs
    {
        public static BaseEventHandler<TEventArgs> operator +(BaseEventHandler<TEventArgs> eventHandler, (object, EventHandler<TEventArgs>) tuple) => eventHandler.Subscribe(tuple);
        public static BaseEventHandler<TEventArgs> operator +(BaseEventHandler<TEventArgs> eventHandler, EventHandler<TEventArgs> @delegate) => eventHandler.Subscribe(@delegate);
        public static BaseEventHandler<TEventArgs> operator -(BaseEventHandler<TEventArgs> eventHandler, EventHandler<TEventArgs> @delegate) => eventHandler.Unsubscribe(@delegate);

        public abstract BaseEventHandler<TEventArgs> Subscribe(object component, EventHandler<TEventArgs> @delegate);
        public abstract BaseEventHandler<TEventArgs> Subscribe((object Component, EventHandler<TEventArgs> Delegate) tuple);
        public abstract BaseEventHandler<TEventArgs> Subscribe(EventHandler<TEventArgs> @delegate);
        public abstract BaseEventHandler<TEventArgs> Unsubscribe(EventHandler<TEventArgs> action);

        protected BaseEventHandler()
        {
            if (!IsSubclassOf(GetType().GetGenericTypeDefinition(), typeof(BaseEventHandlerWithInvoke<>)))
                throw new InvalidCastException($"Do not create custom implementations of {nameof(BaseEventHandler<TEventArgs>)}");
        }
        // TODO: Optimize
        private static bool IsSubclassOf(Type type, Type baseType)
        {
            if (type == null || baseType == null || type == baseType)
                return false;

            var typeTypeInfo = type.GetTypeInfo();
            var baseTypeTypeInfo = baseType.GetTypeInfo();

            if (!baseTypeTypeInfo.IsGenericType)
            {
                if (!typeTypeInfo.IsGenericType)
                    return typeTypeInfo.IsSubclassOf(baseType);
            }
            else
            {
                baseType = baseType.GetGenericTypeDefinition();
                baseTypeTypeInfo = baseType.GetTypeInfo();
            }

            type = typeTypeInfo.BaseType;
            typeTypeInfo = type.GetTypeInfo();

            var objectType = typeof(object);
            while (type != objectType && type != null)
            {
                var curentType = typeTypeInfo.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (curentType == baseType)
                    return true;

                type = typeTypeInfo.BaseType;
                typeTypeInfo = type.GetTypeInfo();
            }

            return false;
        }


        public abstract void Dispose();
    }
    public abstract class BaseEventHandlerWithInvoke<TEventArgs> : BaseEventHandler<TEventArgs> where TEventArgs : EventArgs
    {
        protected internal abstract void Invoke(object sender, TEventArgs e);
    }

    public sealed class CustomEventHandler<TEventArgs> : BaseEventHandlerWithInvoke<TEventArgs> where TEventArgs : EventArgs
    {
        // I would have used ValueTuple, but the comparison should be done only using EventHandler<TEventArgs> Action.
        private class Storage : IEqualityComparer<Storage>
        {
            public readonly object Component;
            public readonly EventHandler<TEventArgs> Delegate;

            public Storage(object component, EventHandler<TEventArgs> @delegate) { Component = component; Delegate = @delegate; }
            public Storage((object Component, EventHandler<TEventArgs> Delegate) tuple) { Component = tuple.Component; Delegate = tuple.Delegate; }


            public bool Equals(Storage x, Storage y) => ((Delegate) x.Delegate).Equals((Delegate) y.Delegate);

            public int GetHashCode(Storage obj) => obj.Component.GetHashCode() ^ ((Delegate) obj.Delegate).GetHashCode();

            public override bool Equals(object obj)
            {
                var storage = obj as Storage;
                return !ReferenceEquals(storage, null) && Equals(this, storage);
            }
            public override int GetHashCode() => GetHashCode(this);
        }

        private List<Storage> Subscribers { get; } = new List<Storage>();

        public override BaseEventHandler<TEventArgs> Subscribe(object component, EventHandler<TEventArgs> @delegate) { lock (Subscribers) { Subscribers.Add(new Storage(component, @delegate)); return this; } }
        public override BaseEventHandler<TEventArgs> Subscribe((object Component, EventHandler<TEventArgs> Delegate) tuple) { lock (Subscribers) { Subscribers.Add(new Storage(tuple)); return this; } }
        public override BaseEventHandler<TEventArgs> Subscribe(EventHandler<TEventArgs> @delegate) { lock (Subscribers) { Subscribers.Add(new Storage(null, @delegate)); return this; } }
        public override BaseEventHandler<TEventArgs> Unsubscribe(EventHandler<TEventArgs> @delegate) { lock (Subscribers) { Subscribers.Remove(new Storage(null, @delegate)); return this; } }

        public override void Dispose()
        {
            if (Subscribers.Any())
            {
                Logger.Log(LogType.Debug, $"Leaking events! {string.Join(",", Subscribers.Select(sub => sub.Component.ToString()))}");
#if DEBUG
                throw new Exception("Leaked events!");
#endif
            }
        }

        protected internal override void Invoke(object sender, TEventArgs e)
        {
            lock (Subscribers)
            {
                var tempList = Subscribers.ToList();
                foreach (var subscriber in tempList)
                {
                    if (subscriber != null)
                    {
                        if (subscriber.Component != null)
                        {
                            //if (subscriber.Component.Enabled)
                                subscriber?.Delegate?.Invoke(sender, e);
                        }
                        else
                            subscriber.Delegate?.Invoke(sender, e);
                    }
                }
            }
        }
    }
    public sealed class CustomEventHandlerOld<TEventArgs> : BaseEventHandlerWithInvoke<TEventArgs> where TEventArgs : EventArgs
    {
        private event EventHandler<TEventArgs> EventHandler;

        public override BaseEventHandler<TEventArgs> Subscribe(object component, EventHandler<TEventArgs> @delegate) { EventHandler += @delegate; return this; }
        public override BaseEventHandler<TEventArgs> Subscribe((object Component, EventHandler<TEventArgs> Delegate) tuple) { EventHandler += tuple.Delegate; return this; }
        public override BaseEventHandler<TEventArgs> Subscribe(EventHandler<TEventArgs> @delegate) { EventHandler += @delegate; return this; }
        public override BaseEventHandler<TEventArgs> Unsubscribe(EventHandler<TEventArgs> @delegate) { EventHandler -= @delegate; return this; }

        protected internal override void Invoke(object sender, TEventArgs e) { EventHandler?.Invoke(sender, e); }

        public override void Dispose()
        {
            if (EventHandler.GetInvocationList().Any())
            {
                Logger.Log(LogType.Debug, $"Leaking events!");
#if DEBUG
                throw new Exception("Leaked events!");
#endif
            }
        }
    }


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

    public class ModuleManagerService : ServerService, IUpdatable
    {
        private List<ServerModule> Modules { get; } = new List<ServerModule>();


        public BaseEventHandler<ClientJoinedEventArgs> ClientJoined = new CustomEventHandler<ClientJoinedEventArgs>();
        public BaseEventHandler<ClientLeavedEventArgs> ClientLeaved = new CustomEventHandler<ClientLeavedEventArgs>();
        

        //public BaseEventHandler<ServerSentMessageEventArgs> ServerSentMessage = new CustomEventHandler<ServerSentMessageEventArgs>();

        public ModuleManagerService(IServiceContainer services, ConfigType configType) : base(services, configType)
        {
            Modules.Add(new ModuleSCON(Services, ConfigType));
            //Modules.Add(new ModuleNPC(this, ConfigType));
            Modules.Add(new ModuleP3D(Services, ConfigType));
            //Modules.Add(new ModulePokeD(this, ConfigType));

            foreach (var module in LoadModules())
                Modules.Add(module);
        }    
        private IEnumerable<ServerModule> LoadModules()
        {
            Logger.Log(LogType.Debug, $"Loading external modules");
            AppDomain.CurrentDomain.AssemblyResolve += AppDomain_AssemblyResolve;

            foreach (var moduleFile in new ModulesFolder().GetModuleFiles())
            {
                Logger.Log(LogType.Debug, $"Loading module {moduleFile.Name}");
                var assembly = moduleFile.GetModule();

                var serverModule = assembly?.ExportedTypes.SingleOrDefault(type => type.GetTypeInfo().IsSubclassOf(typeof(ServerModule)) && !type.GetTypeInfo().IsAbstract);
                if (serverModule != null)
                {
                    Logger.Log(LogType.Debug, $"Created module {serverModule.FullName}");
                    yield return (ServerModule) Activator.CreateInstance(serverModule, new object[] {Services, ConfigType});
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
                    Logger.Log(LogType.Debug, $"Found assembly!");
                    return assembly;
                }
            }
            
            Logger.Log(LogType.Debug, $"Assembly {args.Name} not found!");
            return null;
        }

        public IReadOnlyList<IServerModuleBaseSettings> GetModuleSettings() => Modules;

        public IEnumerable<Client> GetAllClients() => Modules.Where(module => module.ClientsVisible).SelectMany(module => module.GetClients().Where(client => !client.Permissions.HasFlag(PermissionFlags.UnVerified)));
        public Client GetClient(int id) => GetAllClients().FirstOrDefault(client => client.ID == id);
        public Client GetClient(string name) => GetAllClients().FirstOrDefault(client => client.Name == name || client.Nickname == name);
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


            /*
            if (!CurrentTrades.Any(t => t.Equals(sender.ID, destClient.ID)))
                CurrentTrades.Add(new TradeInstance { Client0ID = sender.ID, Client1ID = destClient.ID });

            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(sender.ID, destClient.ID));
            if (trade != null)
            {
                if (trade.Client0ID == sender.ID)
                    trade.Client0Monster = new Monster(monster);

                if (trade.Client1ID == sender.ID)
                    trade.Client1Monster = new Monster(monster);
            }
            */
            Logger.Log(LogType.Trade, $"{sender.Name} sent a trade request to {destClient.Name}. Module {callerModule.GetType().Name}");
        }
        public void TradeConfirm(Client sender, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => serverModule != callerModule))
                module.OnTradeConfirm(sender, destClient);


            /*
            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(sender.ID, destClient.ID));
            if (trade != null)
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
            */
            Logger.Log(LogType.Trade, $"{sender.Name} confirmed a trade request with {destClient.Name}. Module {callerModule.GetType().Name}");
        }
        public void TradeCancel(Client sender, Client destClient, ServerModule callerModule)
        {
            foreach (var module in Modules.Where(serverModule => serverModule != callerModule))
                module.OnTradeCancel(sender, destClient);


            /*
            var trade = CurrentTrades.FirstOrDefault(t => t.Equals(sender.ID, destClient.ID));
            if (trade != null)
                CurrentTrades.Remove(trade);
            */
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
            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, "Stopping Modules...");
            foreach (var module in Modules)
                module.Stop();
            Logger.Log(LogType.Debug, "Stopped Modules.");

            return true;
        }

        public void Update()
        {
            foreach (var module in Modules)
                module.Update();
        }

        public override void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AppDomain_AssemblyResolve;
        }
    }
}
