namespace PokeD.Server.Clients.PokeD
{
    public partial class PokeDPlayer
    {
        private bool ExecuteCommand(string message)
        {
            return Module.ExecuteClientCommand(this, message);
        }
    }
}