namespace PokeD.Server.Clients.NPC
{
    public interface INPC
    {
        bool IsMoving { get; }

        void Move(int x, int y, int z);

        void SayPlayerPM(int playerID, string message);
        void SayGlobal(string message);
    }
}