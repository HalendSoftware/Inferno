using Sandbox;

public static class ChatboxPreferences
{
    public static ChatboxSettings Chat
    {
        get
        {
            if ( _chatSettings is null )
            {
                var file = "/settings/chat.json";
                _chatSettings = FileSystem.Data.ReadJson( file, new ChatboxSettings() );
            }
            return _chatSettings;
        }
    }
    static ChatboxSettings _chatSettings;

    public static void Save()
    {
        var file = "/settings.json";
        FileSystem.Data.WriteJson( file, Chat );
    }

}
public class ChatboxSettings
{
    public bool ShowAvatars { get; set; } = true;
    public int FontSize { get; set; } = 16;
    public bool ChatSounds { get; set; } = true;
}