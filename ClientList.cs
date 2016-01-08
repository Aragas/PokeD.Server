using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PokeD.Core.Data.SCON;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public class ClientList : IEnumerable<IClient>
    {
        public int Count => ClientsList.Count;
        private List<IClient> ClientsList { get; } 


        public ClientList() { ClientsList = new List<IClient>(); }
        
        public IEnumerator<IClient> GetEnumerator() { return ClientsList.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public IEnumerable<T> GetTypeEnumerator<T>() where T : IClient
        {
            return ClientsList.Where(client => client.GetType() == typeof (T)).Select(client => (T) client);
        }
        public IEnumerable<IClient> GetTypeEnumerator<T1, T2>() where T1 : IClient where T2 : IClient
        {
            return ClientsList.Select(client => new {client, type = client.GetType()})
                .Where(t =>
                    t.type == typeof (T1) ||
                    t.type == typeof (T2))
                .Select(t => t.client);
        }
        public IEnumerable<IClient> GetTypeEnumerator<T1, T2, T3>() where T1 : IClient where T2 : IClient where T3 : IClient
        {
            return ClientsList.Select(client => new {client, type = client.GetType()})
                .Where(t =>
                    t.type == typeof (T1) ||
                    t.type == typeof (T2) ||
                    t.type == typeof (T3))
                .Select(t => t.client);
        }
        public IEnumerable<IClient> GetTypeEnumerator<T1, T2, T3, T4>() where T1 : IClient where T2 : IClient where T3 : IClient where T4 : IClient
        {
            return ClientsList.Select(client => new {client, type = client.GetType()})
                .Where(t =>
                    t.type == typeof (T1) ||
                    t.type == typeof (T2) ||
                    t.type == typeof (T3) ||
                    t.type == typeof (T4))
                .Select(t => t.client);
        }

        public IEnumerable<PlayerInfo> GetAllClientsInfo()
        {
            return ClientsList.Select(client => new PlayerInfo
            {
                Name = client.Name,
                IP = client.IP,
                Ping = 0,
                Position = client.Position,
                LevelFile = client.LevelFile,
                PlayTime = DateTime.Now - client.ConnectionTime
            });
        }

        public IClient this[int index] => ClientsList[index];

        public void Add(IClient client) { ClientsList.Add(client); }
        public void Remove(IClient client) { ClientsList.Remove(client); }

        public void Clear() { ClientsList.Clear(); }
    }
}
