using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using PCLExt.Config;

using PokeD.Core;
using PokeD.Core.Services;
using PokeD.Server.Database;
using PokeD.Server.Storage.Files;
using PokeD.Server.Storage.Folders;

using SQLite;

namespace PokeD.Server.Services
{
    public class DatabaseService : BaseServerService
    {
        protected override IConfigFile ServiceConfigFile => new DatabaseComponentConfigFile(ConfigType);

        private SQLiteConnection Database { get; set; }

        #region Settings

        public string DatabaseName { get; private set; } = "Database";

        #endregion

        public DatabaseService(IServiceContainer services, ConfigType configType) : base(services, configType) { }

        public T DatabaseFind<T>(Expression<Func<T, bool>> exp) where T : IDatabaseTable, new() => Database.Find(exp);
        public bool DatabaseFind<T>(object primaryKey) where T : IDatabaseTable, new() => Database.Find<T>(primaryKey) != null;
        public T DatabaseGet<T>(object primaryKey) where T : IDatabaseTable, new() => Database.Find<T>(primaryKey);
        public void DatabaseSet<T>(T obj) where T : IDatabaseTable, new() => Database.Insert(obj);
        public void DatabaseUpdate<T>(T obj) where T : IDatabaseTable, new() => Database.Update(obj);
        public void DatabaseRemove<T>(T obj) where T : IDatabaseTable, new() => Database.Delete(obj);
        public void DatabaseRemove<T>(object primaryKey) where T : IDatabaseTable, new() => Database.Delete<T>(primaryKey);
        public IEnumerable<T> DatabaseGetAll<T>() where T : IDatabaseTable, new() => Database.Table<T>();
        
        public override bool Start()
        {
            Logger.Log(LogType.Debug, $"Loading {DatabaseName}...");
            if (!base.Start())
                return false;

            Database = new SQLiteConnection(Path.Combine(new DatabaseFolder().Path, $"{DatabaseName}.sqlite3"));
            CreateTables();
            Logger.Log(LogType.Debug, $"Loaded {DatabaseName}.");

            return true;
        }
        public override bool Stop()
        {
            Logger.Log(LogType.Debug, $"Unloading {DatabaseName}...");
            if (!base.Stop())
                return false;

            Database?.Dispose();
            Logger.Log(LogType.Debug, $"Unloaded {DatabaseName}.");

            return true;
        }
        private void CreateTables()
        {
            var asm = typeof(Server).GetTypeInfo().Assembly;
            var typeInfos = asm.DefinedTypes.Where(typeInfo => typeInfo.ImplementedInterfaces.Contains(typeof(IDatabaseTable)));
            foreach (var typeInfo in typeInfos)
                Database.CreateTable(typeInfo.AsType());
        }

        public override void Dispose()
        {
            Database?.Dispose();
        }
    }
}