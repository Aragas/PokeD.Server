using PokeD.Core.Data.PokeD;

namespace PokeD.Server.Data
{
    public class TradeInstance
    {
        public int Client0ID { get; set; }
        public Monster Client0Monster { get; set; } 
        public bool Client0Confirmed { get; set; }

        public int Client1ID { get; set; }
        public Monster Client1Monster { get; set; }
        public bool Client1Confirmed { get; set; }

        public bool Equals(int player0ID, int player1ID) => (Client0ID == player0ID || Client0ID == player1ID) || (Client1ID == player0ID || Client1ID == player1ID);
    }
}