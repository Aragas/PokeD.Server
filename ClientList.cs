using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public class ClientList : IEnumerable
    {
        public int Count => Clients.Count;

        private List<IClient> Clients { get; set; } 

        public ClientList()
        {
            Clients = new List<IClient>();
        }

        public IEnumerator GetEnumerator()
        {
            return Clients.GetEnumerator();
        }

        public IEnumerable<T> GetConcreteTypeEnumerator<T>() where T : IClient
        {
            var type = typeof (T);
            var list = new List<T>();
            foreach (var client in Clients)
                if (client.GetType() == type)
                    list.Add((T) client);

            return list.AsEnumerable();
        }

        public IClient this[int index] => Clients[index];

        public void Add(IClient client)
        {
            Clients.Add(client);
        }

        public void Remove(IClient client)
        {
            Clients.Remove(client);
        }

        public string[] GetAllClientsName()
        {
            return Clients.Select(client => client.Name).ToArray();
        }

        public void Clear()
        {
            Clients.Clear();
        }
    }
}
