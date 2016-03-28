using PokeD.Core.Data.PokeD.Monster;

namespace PokeD.Server.Data
{
    public class TradeInstance
    {
        public int Player_0_ID { get; set; }
        public Monster Player_0_Monster { get; set; } 
        public bool Player_0_Confirmed { get; set; }

        public int Player_1_ID { get; set; }
        public Monster Player_1_Monster { get; set; }
        public bool Player_1_Confirmed { get; set; }

        public bool Equals(int player_0_ID, int player_1_ID)
        {
            return (Player_0_ID == player_0_ID || Player_0_ID == player_1_ID) || (Player_1_ID == player_0_ID || Player_1_ID == player_1_ID);
        }
    }
}
