namespace PokeD.Server.Chat
{
    public class GlobalChatChannel : ChatChannel
    {
        public override string Name { get; protected set; } = "Global Chat";
        public override string Description { get; protected set; } = "Global Chat System.";
        public override string Alias { get; protected set; } = "global";
    }
}