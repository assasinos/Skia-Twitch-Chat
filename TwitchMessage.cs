

namespace Skia_Twitch_Chat;

public class TwitchMessage
{
    public string Nickname;
    
    
    
    public string Message;

    public TwitchMessage(string nickname, string message)
    {
        this.Message = message;
        this.Nickname = nickname;
    }
    
    
    public override string ToString()
    {
        
        //Debug only 
        return $"{Nickname} - {Message}";
    }
    
    
}