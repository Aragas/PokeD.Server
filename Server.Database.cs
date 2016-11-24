using System.Diagnostics;
using System.Linq;

using PCLExt.AppDomain;

using PokeD.Server.Clients;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server
    {
        private void CreateTables()
        {
            var asm = AppDomain.GetAssembly(typeof(Server));
            var typeInfos = asm.DefinedTypes.Where(typeInfo => typeInfo.ImplementedInterfaces.Contains(typeof(IDatabaseTable)));
            foreach(var typeInfo in typeInfos)
                Database.CreateTable(typeInfo.AsType());
        }

        public bool DatabaseFind<T>(T obj) where T : class, new()
        {
            return Database.Find<T>(obj) != null;
        }
        public bool DatabaseFind<T>(ref T obj) where T : class, new()
        {
            return (obj = Database.Find<T>(obj)) != null;
        }
        public bool DatabaseSave<T>(T obj) where T : class, new()
        {
            if (!DatabaseFind(obj))
                Database.Insert(obj);
            else
                Database.Update(obj);

            return false;
        }
        public bool DatabaseLoad<T>(ref T obj) where T : class, new()
        {
            if (!DatabaseFind(ref obj))
                return false;

            return true;
        }

        public int DatabasePlayerGetID(Client player)
        {
            if (GetAllClients().Any(p => p.Nickname == player.Nickname))
                return -1;

            var data = Database.Table<ClientTable>().FirstOrDefault(p => p.Name == player.Nickname);
            if (data != null)
            {
                player.ID = data.Id;
                return player.ID;
            }
            else
            {
                Database.Insert(new ClientTable(player));
                return DatabasePlayerGetID(player);
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


        //public bool DatabaseBatteSave(BattleInstance battleInstance)
        //{
        //    Database.Insert(new Battle(battleInstance));
        //    return true;
        //}


        public bool DatabaseTradeSave(TradeInstance tradeInstance)
        {
            Database.Insert(new Trade(Database, tradeInstance));
            return true;
        }

        public bool DatabasePlayerChannelSave(Client player, int channel)
        {
            Database.Insert(new ClientChannelTable(player.ID, channel));
            return true;
        }
        public bool DatabasePlayerChannelLoad(Client player, out int channel)
        {
            channel = 0;

            if (GetAllClients().Any(p => p.Nickname == player.Nickname))
                return false;

            var data = Database.Find<ClientTable>(p => p.Name == player.Nickname);



            Database.Insert(new ClientChannelTable(player.ID, channel));
            return true;
        }
    }
}