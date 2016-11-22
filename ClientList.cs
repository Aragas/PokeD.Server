using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public class ClientList : IEnumerable<Client>
    {
        public int Count => ClientsList.Count;
        private List<Client> ClientsList { get; } = new List<Client>();

        
        public IEnumerator<Client> GetEnumerator() => ClientsList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<T> GetTypeEnumerator<T>() where T : Client =>
            ClientsList.Where(client => client.GetType() == typeof(T)).Select(client => (T) client);
        public IEnumerable<Client> GetTypeEnumerator<T1, T2>() where T1 : Client where T2 : Client =>
            ClientsList.Select(client => new { client, type = client.GetType() })
                .Where(t =>
                    t.type == typeof(T1) ||
                    t.type == typeof(T2))
                .Select(t => t.client);
        public IEnumerable<Client> GetTypeEnumerator<T1, T2, T3>() where T1 : Client where T2 : Client where T3 : Client =>
            ClientsList.Select(client => new { client, type = client.GetType() })
                .Where(t =>
                    t.type == typeof(T1) ||
                    t.type == typeof(T2) ||
                    t.type == typeof(T3))
                .Select(t => t.client);
        public IEnumerable<Client> GetTypeEnumerator<T1, T2, T3, T4>() where T1 : Client where T2 : Client where T3 : Client where T4 : Client =>
            ClientsList.Select(client => new { client, type = client.GetType() })
                .Where(t =>
                    t.type == typeof(T1) ||
                    t.type == typeof(T2) ||
                    t.type == typeof(T3) ||
                    t.type == typeof(T4))
                .Select(t => t.client);

        public Client this[int index] => ClientsList[index];

        public void Add(Client client) => ClientsList.Add(client);
        public void Remove(Client client) => ClientsList.Remove(client);

        public void Clear() => ClientsList.Clear();
    }
}