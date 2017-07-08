using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

using Aragas.Network.Data;
using Aragas.Network.Packets;

using PokeD.Core;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Server.Chat;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Commands;

namespace PokeD.Server.Clients
{
    public abstract class Client : IUpdatable, IDisposable
    {
        public event EventHandler Ready;
        public event EventHandler Disconnected;

        public abstract int ID { get; set; }

        public string Name => Prefix != Prefix.NONE ? $"[{Prefix}] {Nickname}" : Nickname;
        public abstract string Nickname { get; protected set; }
        public abstract Prefix Prefix { get; protected set; }
        public abstract string PasswordHash { get; set; }
        public abstract Vector3 Position { get; set; }
        public abstract string LevelFile { get; set; }

        public abstract PermissionFlags Permissions { get; set; }
        public abstract string IP { get; }
        public abstract DateTime ConnectionTime { get; }
        public abstract CultureInfo Language { get; }

        protected CancellationTokenSource UpdateToken { get; set; }
        protected ManualResetEventSlim UpdateLock { get; } = new ManualResetEventSlim(false);
        
        protected ManualResetEventSlim ConnectionLock { get; } = new ManualResetEventSlim(true); // Will cause deadlock if false. See Leave();

        private ServerModule Module { get; }


        protected Client(ServerModule serverModule) { Module = serverModule; }


        public void StartListening()
        {
            if (!UpdateLock.IsSet)
            {
                UpdateToken = new CancellationTokenSource();
                new Thread(Update).Start();
            }
            else
                throw new Exception("UpdateThread is already running!");
        }

        protected void Join() => Ready?.Invoke(this, EventArgs.Empty);
        protected void Leave()
        {
            ConnectionLock.Wait(); // this should ensure we will send every packet enqueued at the moment of calling Leave()

            if (UpdateToken?.IsCancellationRequested == false)
            {
                UpdateToken.Cancel();
                UpdateLock.Wait(); // Wait for the Update cycle to finish
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public virtual bool RegisterOrLogIn(string passwordHash)
        {
            ClientTable table;
            if ((table = Module.Database.DatabaseGet<ClientTable>(ID)) != null)
            {
                if (table.PasswordHash == null)
                {
                    PasswordHash = passwordHash;
                    Save(true);
                    return true;
                }

                return table.PasswordHash == passwordHash;
            }
            return false;
        }
        public virtual bool ChangePassword(string oldPasswordHash, string newPasswordHash)
        {
            if (string.Equals(PasswordHash, oldPasswordHash, StringComparison.Ordinal))
            {
                PasswordHash = newPasswordHash;
                Save(true);

                return true;

            }

            return false;
        }

        public abstract GameDataPacket GetDataPacket();

        public abstract void SendPacket(Packet packet);

        public abstract void SendChatMessage(ChatChannelMessage chatMessage);
        public abstract void SendPrivateMessage(ChatMessage chatMessage);
        public abstract void SendServerMessage(string text);

        /// <summary>
        /// Will raise Disconnected event.
        /// </summary>
        /// <param name="reason"></param>
        public virtual void SendKick(string reason = "") { Leave(); }
        /// <summary>
        /// Will raise Disconnected event.
        /// </summary>
        /// <param name="banTable"></param>
        public virtual void SendBan(BanTable banTable) { Leave(); }

        Stopwatch UpdateWatch { get; } = Stopwatch.StartNew();
        public virtual void Save(bool force = false)
        {
            if (ID == 0)
                return;

            if (force || UpdateWatch.ElapsedMilliseconds >= 2000)
            {
                Module.Database.DatabaseUpdate(new ClientTable(this));

                UpdateWatch.Reset();
                UpdateWatch.Start();
            }
        }
        public virtual void Load(ClientTable data)
        {
            if (data.ClientID.HasValue)
                ID = data.ClientID.Value;
        }

        public abstract void Update();

        public virtual void Dispose()
        {
            if (UpdateToken?.IsCancellationRequested == false)
            {
                UpdateToken.Cancel();
                UpdateLock.Wait();
            }

            UpdateLock.Dispose();
            ConnectionLock.Dispose();
        }
    }

    public abstract class Client<TServerModule> : Client where TServerModule : ServerModule
    {
        protected TServerModule Module { get; }

        protected Client(TServerModule serverModule) : base(serverModule) { Module = serverModule; }
    }
}