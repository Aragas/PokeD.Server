using System;

using PokeD.Server.Clients;

using SQLite;

namespace PokeD.Server.Database
{
    public sealed class BanTable : IDatabaseTable
    {
        [PrimaryKey]
        public int ClientID { get; set; }

        public DateTime UnbanTime { get; set; }
        public string Reason { get; set; }

        public BanTable() { }
        public BanTable(Client client, DateTime unbanTime, string reason)
        {
            ClientID = client.ID;

            UnbanTime = unbanTime;
            Reason = reason;
        }
    }
}