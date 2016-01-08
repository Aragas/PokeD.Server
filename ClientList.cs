using System;
using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.SCON;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public class ClientList
    {
        public int Count => ClientsList.Count;

        private List<IClient> ClientsList { get; } 

        public ClientList()
        {
            ClientsList = new List<IClient>();
        }

        public IEnumerable<IClient> GetEnumerator()
        {
            return ClientsList.AsEnumerable();
        }

        public IEnumerable<T> GetConcreteTypeEnumerator<T>() where T : IClient
        {
            var type = typeof (T);
            var list = new List<T>();
            foreach (var client in ClientsList)
                if (client.GetType() == type)
                    list.Add((T) client);

            return list.AsEnumerable();
        }
        public IEnumerable<IClient> GetConcreteTypeEnumerator<T1, T2>() where T1 : IClient where T2 : IClient
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);

            var list = new List<IClient>();
            foreach (var client in ClientsList)
            {
                var type = client.GetType();
                if (type == type1 || type == type2)
                    list.Add(client);
            }
            
            return list.AsEnumerable();
        }
        public IEnumerable<IClient> GetConcreteTypeEnumerator<T1, T2, T3>() where T1 : IClient where T2 : IClient where T3 : IClient
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);
            var type3 = typeof(T3);

            var list = new List<IClient>();
            foreach (var client in ClientsList)
            {
                var type = client.GetType();
                if (type == type1 || type == type2 || type == type3)
                    list.Add(client);
            }

            return list.AsEnumerable();
        }

        public IClient this[int index] => ClientsList[index];

        public void Add(IClient client)
        {
            ClientsList.Add(client);
        }
        public void Remove(IClient client)
        {
            ClientsList.Remove(client);
        }

        public PlayerInfo[] GetAllClientsInfo()
        {
            var list = new List<PlayerInfo>();
            foreach (var c in ClientsList)
                list.Add(new PlayerInfo
                {
                    Name = c.Name,
                    GameJoltID = c.GameJoltID,
                    IP = c.IP,
                    Ping = 0,
                    Position = c.Position,
                    LevelFile = c.LevelFile,
                    PlayTime = DateTime.Now - c.ConnectionTime
                });
            return list.ToArray();
        }

        public void Clear()
        {
            ClientsList.Clear();
        }
    }
}
