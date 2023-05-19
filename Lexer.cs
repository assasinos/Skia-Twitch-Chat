using System.Net.Sockets;

namespace Skia_Twitch_Chat;

public class Lexer
{
    

    public static List<TwitchMessage> ParseMessage(string line)
    {
        List<TwitchMessage> list = new ();


        var updatedLine = line.Replace("PRIVMSG", "");
        string[]? segments;
        string? nickname;
    
    
        //Multiple Messages
        if (line.Contains("\r\n"))
        {
            foreach (var text in line.Split("\r\n"))
            {
                segments = updatedLine.Split(" ");
                nickname = segments[1].Replace(":","").Split("!")[0].ToLower();
                list.Add(new(message: string.Join(" ", segments.Skip(3)), nickname: nickname));
            }
            return list;
        }
        
        //Single Message
        segments = updatedLine.Split(" ");
        nickname = segments[1].Replace(":","").Split("!")[0].ToLower();

        list.Add(new($"<{nickname}> :", string.Join(" ", segments.Skip(4)).Remove(0, 1)));
        return list;
    }

}