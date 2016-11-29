using System;
using System.Diagnostics;
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


        // Find can return null, Get will throw exception if not found
        public bool DatabaseFind<T>(object primaryKey) where T : class, new() => Database.Find<T>(primaryKey) != null;
        public bool DatabaseFind<T>(Expression<Func<T, bool>> exp) where T : class, new() => Database.Find(exp) != null;
        public void DatabaseSave<T>(T obj) where T : class, new() => Database.Insert(obj);
        //public T DatabaseLoad<T>(object primaryKey) where T : class, new() => Database.Get<T>(primaryKey);
        public T DatabaseLoad<T>(object primaryKey) where T : class, new() => Database.Find<T>(primaryKey);
        public void DatabaseUpdate<T>(T obj) where T : class, new() => Database.Update(obj);


        public bool DatabaseSetClientId(Client player)
        {
            if (GetAllClients().Any(p => p.Nickname == player.Nickname))
                return false;

            var data = Database.Table<ClientTable>().FirstOrDefault(p => p.Name == player.Nickname);
            if (data != null)
            {
                player.ID = data.Id;
                return true;
            }
            else
            {
                Database.Insert(new ClientTable(player));
                return DatabaseSetClientId(player);
            }
        }

        Stopwatch DatabasePlayerWatch { get; } = Stopwatch.StartNew();
        public void DatabasePlayerSave(Client player, bool forceUpdate = false)
        {
            if (player.ID == 0)
                return;

            if (DatabasePlayerWatch.ElapsedMilliseconds < 2000 && !forceUpdate)
                return;

            Database.Update(new ClientTable(player));

            DatabasePlayerWatch.Reset();
            DatabasePlayerWatch.Start();
        }

        public bool DatabasePlayerLoad(Client player)
        {
            if (GetAllClients().Any(p => p.Nickname == player.Nickname))
                return false;

            var data = Database.Find<ClientTable>(p => p.Name == player.Nickname);


            if (data != null && data.PasswordHash == null)
            {
                Database.Update(new ClientTable(player));
                return true;
            }
            else if (data != null)
            {
                if (data.PasswordHash == player.PasswordHash)
                {
                    player.LoadFromDB(data);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                Database.Insert(new ClientTable(player));
                player.LoadFromDB(Database.Find<ClientTable>(p => p.Name == player.Nickname));

                return true;
            }
        }
    }
}