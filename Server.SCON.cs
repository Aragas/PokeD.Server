using System.Collections.Generic;

using PokeD.Server.Clients;

namespace PokeD.Server
{
    public partial class Server
    {
        List<IClient> SCONClients { get; } = new List<IClient>();

        public void AddSCON(IClient scon)
        {
            SCONClients.Add(scon);

        }
        public void RemoveSCON(IClient scon)
        {
            SCONClients.Remove(scon);
        }
    }
}
