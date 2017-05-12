using PokeD.Core.Data.PokeD;

namespace PokeD.Server.Database
{
    public class TradeInstance
    {
        public int Client0ID { get; set; }
        public Monster Client0Monster { get; set; } 
        public bool Client0Confirmed { get; set; }

        public int Client1ID { get; set; }
        public Monster Client1Monster { get; set; }
        public bool Client1Confirmed { get; set; }

        public bool Equals(int player_0_ID, int player_1_ID) =>
            (Client0ID == player_0_ID || Client0ID == player_1_ID) || (Client1ID == player_0_ID || Client1ID == player_1_ID);
    }
}