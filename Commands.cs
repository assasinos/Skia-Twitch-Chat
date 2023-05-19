using System.Net.Sockets;

namespace Skia_Twitch_Chat;

public class Commands 
{
    private const string ip = "irc.chat.twitch.tv";
    private const int port = 6667;

    [Command("join","Join's channel with specified username")]
    public async Task Join(CommandContext ctx,string? channel)
    {
        if (ctx.Channel is not null) return;

        ctx.Window.Channel = channel;
        
        ctx.Window.Window.Title = $"{channel}'s chat";

        
        //Connect
        if (!ctx.Window.tcpClient.Connected)
        {
            try
            {
                var password = "oauth:" + (Environment.GetEnvironmentVariable("TwitchOathPassword") ?? throw new Exception("Missing Oath Password"));
                var botUsername = Environment.GetEnvironmentVariable("TwitchOathNickname") ?? throw new Exception("Missing Oath Username");
                ctx.Window.botUsername = botUsername;
                await ctx.Window.tcpClient.ConnectAsync(ip,port);
                ctx.Window.streamReader = new StreamReader(ctx.TcpClient.GetStream());
                ctx.Window.streamWriter = new StreamWriter(ctx.TcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };

                await ctx.Window.streamWriter.WriteLineAsync($"CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands");
                await ctx.Window.streamWriter.WriteLineAsync($"PASS {password}");
                await ctx.Window.streamWriter.WriteLineAsync($"NICK {botUsername}");
                await ctx.Window.streamWriter.WriteLineAsync($"JOIN #{channel}");
            
                ctx.Window.ReadFromIRC();
                await ctx.Window.AddMessage(new($"<{ctx.Window.Channel}>", "---Connected---"));
                return;
            }
            catch (Exception e)
            {
                await ctx.Window.AddMessage(new TwitchMessage($"<{channel}>", "Could Not Connect Retrying in 1 s Check Console for more info"));
                Console.WriteLine(e);
                await Task.Delay(1000);
                ctx.Window.Channel = null;
                
                Join(ctx,channel);
                return;
            }

        }
        await ctx.Window.streamWriter.WriteLineAsync($"JOIN #{channel}");
        await ctx.Window.AddMessage(new($"<{ctx.Window.Channel}>", "---Connected---"));
    }
    [Command("leave", "Leaves current channel")]
    public async Task Leave(CommandContext ctx)
    {
        if (ctx.Channel is  null || !ctx.Window.tcpClient.Connected) return;
        
        
        await ctx.Window.AddMessage(new($"<{ctx.Window.Channel}>", "---Disconected---"));
        ctx.Window.Channel = null;
        await ctx.Window.streamWriter.WriteLineAsync($"PART #{ctx.Channel}");
        ctx.Window.Window.Title = $"Not Connected";
        
    }
    [Command("clear", "Clear's all messages (localy)")]
    public async Task Clear(CommandContext ctx)
    {
        await ctx.Window.WatingQueue.WaitAsync();
        await ctx.Window.ClearMessages();
        ctx.Window.WatingQueue.Release();
    }
    [Command("exit", "Closes app")]
    public async Task Exit(CommandContext ctx)
    {
        await ctx.Window.WatingQueue.WaitAsync();
        ctx.Window.Window.Close();
        ctx.Window.WatingQueue.Release();
        
    }
    
    [Command("help", "Shows this help")]
    public async Task Help(CommandContext ctx)
    {
        await ctx.Window.AddMessage(new ("<Help>","All Commands:"));
        var methods = typeof(Commands).GetMethods();
        foreach (var method in methods)
        {
            //GetCustomAttributes(typeof(CommandAttribute),false).Cast<CommandAttribute>().Any(v => v.Value == command[0].ToLower().Replace("\\","")
            var attribute = method.GetCustomAttributes(typeof(CommandAttribute), false).Cast<CommandAttribute>().FirstOrDefault();
            
            await ctx.Window.AddMessage(new ($"<\\{attribute.Value}>",$"{attribute.Description}"));
            await Task.Delay(10);
        }
    }
}

public class CommandContext
{
    public TcpClient? TcpClient { get; set; }
    public string? Channel { get; set; }

    public ScreenWindow Window { get; set; }

    public CommandContext(TcpClient? tcpClient, string? channel, ScreenWindow window)
    {
        TcpClient = tcpClient;
        Channel = channel;
        Window = window;
    }
}

public class CommandAttribute : Attribute
{
    public string Value { get; }
    public string Description { get; }
    public CommandAttribute(string value, string description = "")
    {
        Value = value.ToLower();
        Description = description;
    }
}