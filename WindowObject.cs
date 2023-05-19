
using Silk.NET.Maths;
using SkiaSharp;


namespace Skia_Twitch_Chat;



class WindowObject : ScreenWindow
{
    

    private static DateTime _lastDrawn = DateTime.Now;
    



    public override void OnRender(double obj)
    {
        DrawOneFrame();

        
    }

    private void DrawInputField()
    {
        SkCanvas.DrawText(inputstring, new SKPoint(10, Window.Size.Y-5), MessagePen);

        var elapsed = (DateTime.Now - _lastDrawn);
        if (elapsed.TotalSeconds > .25)
        {
            var rect = new SKRect();
            MessagePen.MeasureText(inputstring, ref rect);
            SkCanvas.DrawText("|", new SKPoint(10 + rect.Width  , Window.Size.Y-5), MessagePen);
            
        }

        if (elapsed.TotalSeconds > .5)
        {
            _lastDrawn = DateTime.Now;
        }
        
        SkCanvas.Flush();
    }


    private void DrawOneFrame()
    {
        
        
        
        
        if (Messages.Count > MaxMessages)
        {
            Messages = Messages.Skip(Messages.Count - MaxMessages).ToList();
        }
        
        SkCanvas.Clear(SKColor.Parse("#1E1E1E"));


        WatingQueue.Wait();
        foreach (var message in Messages)
        {
            //SkCanvas.DrawText(message.Message.GetNickname(), message.OriginPoint, MessagePen);
            WriteMessage(message);
        }

        
        SkCanvas.Flush();
        WatingQueue.Release();
        
        DrawInputField();

    }
    
    private void WriteMessage((TwitchMessage text, SKPoint drawOrigin) message)
    {
        
        //Write Nickname
        var rect = new SKRect();
        var nickname = message.text.Nickname;
        NicknamePen.MeasureText(nickname, ref rect);
        SkCanvas.DrawText(nickname, message.drawOrigin, NicknamePen);
        //Write Message
        SkCanvas.DrawText(message.text.Message, new (message.drawOrigin.X + rect.Width + 7, message.drawOrigin.Y), MessagePen);
    }


    public  WindowObject(Vector2D<int> size) : base(size)
    {
    }


    
    
    
    
    
    
    
}
