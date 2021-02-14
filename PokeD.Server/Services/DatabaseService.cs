using Microsoft.Extensions.Hosting;

using PokeD.Core;
using PokeD.Server.Database;
using PokeD.Server.Storage.Folders;

using SQLite;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PokeD.Server.Services
{
    public sealed class DatabaseService : IHostedService, IDisposable
    {
        private SQLiteConnection Database { get; set; }

        #region Settings

        public string DatabaseName { get; private set; } = "Database";

        #endregion

        private readonly ILogger _logger;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public T DatabaseFind<T>(Expression<Func<T, bool>> exp) where T : IDatabaseTable, new() => Database.Find(exp);
        public T DatabaseGet<T>(object primaryKey) where T : IDatabaseTable, new() => Database.Find<T>(primaryKey);
        public void DatabaseSet<T>(T obj) where T : IDatabaseTable, new() => Database.Insert(obj);
        public void DatabaseUpdate<T>(T obj) where T : IDatabaseTable, new() => Database.Update(obj);
        public void DatabaseRemove<T>(T obj) where T : IDatabaseTable, new() => Database.Delete(obj);
        public void DatabaseRemove<T>(object primaryKey) where T : IDatabaseTable, new() => Database.Delete<T>(primaryKey);
        public IEnumerable<T> DatabaseGetAll<T>() where T : IDatabaseTable, new() => Database.Table<T>();

        private void CreateTables()
        {
            var asm = typeof(DatabaseService).Assembly;
            var typeInfos = asm.DefinedTypes.Where(typeInfo => typeInfo.ImplementedInterfaces.Contains(typeof(IDatabaseTable)));
            foreach (var typeInfo in typeInfos)
                Database.CreateTable(typeInfo.AsType());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Loading {DatabaseName}...");
            Database = new SQLiteConnection(Path.Combine(new DatabaseFolder().Path, $"{DatabaseName}.sqlite3"));
            CreateTables();
            _logger.LogDebug($"Loaded {DatabaseName}.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.Log(LogType.Debug, $"Unloading {DatabaseName}...");
            //Database?.Dispose();
            //_logger.Log(LogType.Debug, $"Unloaded {DatabaseName}.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Database?.Dispose();
        }
    }
}