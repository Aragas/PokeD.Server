using Aragas.Network.Packets;

using PokeD.Core;
using PokeD.Core.Data;
using PokeD.Core.Packets.P3D.Shared;
using PokeD.Server.Chat;
using PokeD.Server.Commands;
using PokeD.Server.Data;
using PokeD.Server.Database;
using PokeD.Server.Modules;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.Clients
{
    public delegate void ClientEventHandler(Client sender, EventArgs e);

    public abstract class Client : IUpdatable, IDisposable
    {
        public event ClientEventHandler Ready;
        public event ClientEventHandler Disconnected;

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

        protected CancellationTokenSource? UpdateToken { get; set; } = null;
        protected Task UpdateTask { get; set; }

        protected ManualResetEventSlim ConnectionLock { get; } = new(true); // Will cause deadlock if false. See Leave();

        private ServerModule Module { get; }

        private bool IsDisposing { get; set; }


        protected Client(ServerModule serverModule) => Module = serverModule;


        public void StartListening()
        {
            if (UpdateToken is null)
            {
                UpdateToken = new CancellationTokenSource();
                UpdateTask = UpdateAsync(UpdateToken.Token);
            }
            else
                throw new Exception("UpdateTask is already running!");
        }

        protected void Join() => Ready?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// We do not need to call Leave() synchronously because of lock usages.
        /// Calling Leave() from the Update() cycle will cause a deadlock, this is the fix for it.
        /// </summary>
        protected async Task LeaveAsync(CancellationToken ct)
        {
            try
            {
                // Signal cancellation to the executing method
                UpdateToken.Cancel();
            }
            finally
            {
                if (UpdateTask is not null)
                {
                    // Wait until the task completes or the stop token triggers
                    await Task.WhenAny(UpdateTask, Task.Delay(Timeout.Infinite, ct));
                }
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

        public abstract GameDataPacket? GetDataPacket();

        public abstract void SendPacket<TPacket>(TPacket packet) where TPacket : Packet;

        public abstract void SendChatMessage(ChatChannel chatChannel, ChatMessage chatMessage);
        public abstract void SendPrivateMessage(ChatMessage chatMessage);
        public abstract void SendServerMessage(string text);

        /// <summary>
        /// Will raise Disconnected event.
        /// </summary>
        /// <param name="reason"></param>
        public virtual async void SendKick(string reason = "") { await LeaveAsync(CancellationToken.None); }
        /// <summary>
        /// Will raise Disconnected event.
        /// </summary>
        /// <param name="banTable"></param>
        public virtual async void SendBan(BanTable banTable) { await LeaveAsync(CancellationToken.None); }

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

        public abstract Task UpdateAsync(CancellationToken ct);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposing)
            {
                if (disposing)
                {
                    ConnectionLock.Dispose();
                }


                IsDisposing = true;
            }
        }
        ~Client()
        {
            Dispose(false);
        }
    }

    public abstract class Client<TServerModule> : Client where TServerModule : ServerModule
    {
        protected TServerModule Module { get; }

        protected Client(TServerModule serverModule) : base(serverModule) => Module = serverModule;
    }
}