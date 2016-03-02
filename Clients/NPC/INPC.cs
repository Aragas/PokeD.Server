using Aragas.Core.Data;

namespace PokeD.Server.Clients.NPC
{
    public interface INPC
    {
        string LevelFile { get; }
        
        int Facing { get; }
        Vector3 Position { get; }
        string Skin { get; }

        bool PokemonVisible { get; }
        int PokemonFacing { get; }
        Vector3 PokemonPosition { get; }
        string PokemonSkin { get; }

        bool Moving { get; }

        void Move(int x, int y, int z);

        void SayPlayerPM(IClient client, string message);
        void SayGlobal(string message);
    }
}