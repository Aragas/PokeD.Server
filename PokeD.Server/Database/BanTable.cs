using System;

using PokeD.Server.Clients;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class BanTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientID { get; private set; }

        public DateTime UnbanTime { get; private set; }
        public string Reason { get; private set; }

        public BanTable() { }
        public BanTable(Client client, DateTime unbanTime, string reason)
        {
            ClientID = client.ID;

            UnbanTime = unbanTime;
            Reason = reason;
        }
    }
}