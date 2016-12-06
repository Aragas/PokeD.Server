using PokeD.Core.Data.PokeD.Monster;

namespace PokeD.Server.Database
{
    public class TradeInstance
    {
        public int Client0Id { get; set; }
        public Monster Client0Monster { get; set; } 
        public bool Client0Confirmed { get; set; }

        public int Client1Id { get; set; }
        public Monster Client1Monster { get; set; }
        public bool Client1Confirmed { get; set; }

        public bool Equals(int player_0_Id, int player_1_Id) =>
            (Client0Id == player_0_Id || Client0Id == player_1_Id) || (Client1Id == player_0_Id || Client1Id == player_1_Id);
    }
}