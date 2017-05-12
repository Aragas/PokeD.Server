using System;
using System.Linq;
using System.Linq.Expressions;

using PCLExt.AppDomain;

using PokeD.Server.Clients;
using PokeD.Server.Database;

using SQLite;

namespace PokeD.Server
{
    public partial class Server
    {
        private SQLiteConnection Database { get; set; }

        private void CreateTables()
        {
            var asm = AppDomain.GetAssembly(typeof(Server));
            var typeInfos = asm.DefinedTypes.Where(typeInfo => typeInfo.ImplementedInterfaces.Contains(typeof(IDatabaseTable)));
            foreach(var typeInfo in typeInfos)
                Database.CreateTable(typeInfo.AsType());
        }

        public bool DatabaseFind<T>(Expression<Func<T, bool>> exp) where T : class, new() => Database.Find(exp) != null;
        public bool DatabaseFind<T>(object primaryKey) where T : class, new() => Database.Find<T>(primaryKey) != null;
        public T DatabaseGet<T>(object primaryKey) where T : class, new() => Database.Find<T>(primaryKey); // -- Find can return null, Get will throw exception if not found
        public void DatabaseSet<T>(T obj) where T : class, new() => Database.Insert(obj);
        public void DatabaseUpdate<T>(T obj) where T : class, new() => Database.Update(obj);

        public bool DatabaseSetClientID(Client player)
        {
            if (GetAllClients().Any(p => p != player && p.Nickname == player.Nickname))
                return false;

            var data = Database.Table<ClientTable>().FirstOrDefault(p => p.Name == player.Nickname);
            if (data != null)
            {
                player.ID = data.ID;
                return true;
            }
            else
            {
                Database.Insert(new ClientTable(player));
                return DatabaseSetClientID(player);
            }
        }
    }
}