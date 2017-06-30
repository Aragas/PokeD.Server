namespace PokeD.Server.Chat
{
    public class GlobalChatChannel : ChatChannel
    {
        public override string Name => "Global Chat";
        public override string Description => "Global Chat System.";
        public override string Alias => "global";
    }
}