using System.Diagnostics;
using System.Linq;

using PokeD.Server.Clients;
using PokeD.Server.Database;

namespace PokeD.Server
{
    public partial class Server
    {
        public int PeekDBID(IClient player)
        {
            if (AllClients().Any(p => p.Name == player.Name))
                return -1;

            var data = Database.Find<Player>(p => p.Name == player.Name);
            if (data != null)
            {
                player.ID = data.Id;
                return player.ID;
            }
            else
            {
                Database.Insert(new Player(player));
                return PeekDBID(player);
            }
        }

        public bool LoadDBPlayer(IClient player)
        {
            if (AllClients().Any(p => p.Name == player.Name))
                return false;

            var data = Database.Find<Player>(p => p.Name == player.Name);


            if (data != null && data.PasswordHash == null)
            {
                Database.Update(new Player(player));
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
                Database.Insert(new Player(player));
                player.LoadFromDB(Database.Find<Player>(p => p.Name == player.Name));

                return true;
            }
        }

        Stopwatch UpdateDBPlayerWatch { get; } = Stopwatch.StartNew();
        public void UpdateDBPlayer(IClient player, bool forceUpdate = false)
        {
            if (player.ID == 0)
                return;

            if (UpdateDBPlayerWatch.ElapsedMilliseconds < 2000 && !forceUpdate)
                return;

            Database.Update(new Player(player));

            UpdateDBPlayerWatch.Reset();
            UpdateDBPlayerWatch.Start();
        }
    }
}
