namespace PokeD.Server.Clients.NPC
{
    public abstract class NPC
    {
        public abstract bool IsMoving { get; }

        public abstract void Move(int x, int y, int z);

        public abstract void SayPlayerPM(int playerID, string message);
        public abstract void SayGlobal(string message);
    }
}