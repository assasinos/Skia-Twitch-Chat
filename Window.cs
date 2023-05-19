using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Skia_Twitch_Chat;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using SkiaSharp;

public abstract class ScreenWindow : IDisposable
{
    #region Setup

    protected  int MaxMessages = 31;
    private Vector2D<int> _lastsize;
    public string? Channel;
    protected List<(TwitchMessage text, SKPoint drawOrigin)> Messages = new ();
    protected SKPaint NicknamePen;
    protected SKPaint MessagePen;
    public IWindow Window;
    private GRContext GrContext;
    private SKSurface SkSurface;
    protected SKCanvas SkCanvas;

    private  Commands _commands; 
    
    public ScreenWindow(Vector2D<int> size)
    {
        
        _lastsize = size;

        var options = WindowOptions.Default;
        options.Size = size;
        
        options.Title = $"Chatty";
        options.PreferredStencilBufferBits = 8;
        options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);
        
        GlfwWindowing.Use();
        Window = Silk.NET.Windowing.Window.Create(options);
        Window.Load += WindowOnLoad;
        Window.Initialize();
        
        using var grGlInterface = GRGlInterface.Create((name => Window.GLContext!.TryGetProcAddress(name, out var addr) ? addr : (IntPtr) 0));
        grGlInterface.Validate();
        
        GrContext = GRContext.CreateGl(grGlInterface);
        var renderTarget = new GRBackendRenderTarget(size.X, size.Y, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
        SkSurface = SKSurface.Create(GrContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        SkCanvas = SkSurface.Canvas;


        Window.Run();
        
        //Doesn't go here
        
    }

    private void WindowOnLoad()
    {
        Console.WriteLine("load");
        Window.Render += OnRender;
        Window.Closing += OnClose;
        Window.Resize += WindowOnResize;
        
        
        //input?
        var input =  Window.CreateInput();
        var keyboard = input.Keyboards.FirstOrDefault() ?? throw new Exception("Keyboard not found");
        keyboard.KeyChar += KeyboardOnCharPres;
        keyboard.KeyDown += KeyboardOnKeyDown;
        //keyboard.KeyUp += KeyboardOnKeyUp;
        
        inputstring = string.Empty;


        //register Commands
        _commands = new();

        MessagePen = new();
        MessagePen.Color = SKColors.White;
        NicknamePen = new();
        NicknamePen.Color = SKColors.Red;


    }
    
    private void KeyboardOnKeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        switch (arg2)
        {
            case Key.Enter:
                ParseInput();
                return;
            case Key.Space:
                inputstring += " ";
                return;
            case Key.Backspace:
                inputstring = String.Join("",inputstring.Take(inputstring.Length - 1)) ?? "";
                return;
        }


        /*if (arg2 != Key.Backspace) return;
        backspacePressed = true;
        Task.Run(async () =>
        {
            while (backspacePressed)
            {
                inputstring = String.Join("",inputstring.Take(inputstring.Length - 1)) ?? "";
                Thread.Sleep(50);
            }
        });*/
    }

    
    private void ParseInput()
    {
        if (inputstring.StartsWith("\\"))
        {
            Task.Run(async () =>
            {
                var command = inputstring.Split(" ");
                command = command.Where(e => e != "").ToArray();
                var method = typeof(Commands).GetMethods().FirstOrDefault(
                    x =>x.GetCustomAttributes(typeof(CommandAttribute),false).Cast<CommandAttribute>().Any(v => v.Value == command[0].ToLower().Replace("\\","")));
                


                if (method is null)
                {
                    await AddMessage(new TwitchMessage(botUsername, $"Command {command[0]} not found, if you want to send \\ type it twice '\\\\'"));
                    return;
                }

                var numofparameters = method.GetParameters().Length -1 ;

                if (numofparameters != command.Length - 1)
                {
                    await AddMessage(new(botUsername, $"Command Expected {numofparameters} parameters but got {command.Length - 1}"));
                    return;
                }

                var parameters = new object?[numofparameters+1];
                parameters[0] = new CommandContext(tcpClient,Channel, this);
                command = command.Skip(1).ToArray();
                
                for (int i = 1; i < numofparameters+1; i++)
                {
                    parameters[i] = command[i-1];
                }
                //AddMessage(new TwitchMessage(botUsername, $"<Command>[{inputstring}]"));
                
                method.Invoke(_commands,parameters);
                
                inputstring = string.Empty;
                

            });

                return;
            
        }

        if (inputstring.Contains("\\\\")) inputstring.Replace("\\\\", "\\");
            SendMessage();

    }

    public async Task AddMessage(TwitchMessage twitchMessage)
    {
        SKRect rect = new();
        MessagePen.MeasureText($"<{twitchMessage.Nickname}>: {twitchMessage.Message}", ref rect);
        
        
        
        //Check for word wrap
         if (rect.Width > Window.Size.X)
         {
             MessagePen.MeasureText($"{twitchMessage.Nickname}", ref rect);
             //200 pretty random value that works
             var nicknamewidth = rect.Width + 250;
             var width = nicknamewidth;
             var line = string.Empty;
             (TwitchMessage text, SKPoint drawOrigin) mess = new ();       
             foreach (var word in twitchMessage.Message.Split(" ").Where(x => string.Empty != x))
             {

                 var spaceappended = $" {word}";
                 
                 MessagePen.MeasureText(spaceappended, ref rect);
              
                 //200 is a random value
                 if (width + rect.Width < Window.Size.X)
                 {
                     width += rect.Width;
                     line += $" {spaceappended}";
                     continue;
                 }
                 await WatingQueue.WaitAsync();
                 mess = await PrepareText(new TwitchMessage(twitchMessage.Nickname,line));
                 Messages.Add(mess);
                 WatingQueue.Release();
                 width = nicknamewidth;
                 line = spaceappended;
                 
             }
                     
             await WatingQueue.WaitAsync();
             mess = await PrepareText(new TwitchMessage(twitchMessage.Nickname,line));
             Messages.Add(mess);
             WatingQueue.Release();       
                 return;    
         }
        await WatingQueue.WaitAsync();
        var message =await  PrepareText(twitchMessage);
        Messages.Add(message);
        WatingQueue.Release();

    }
    public async Task AddMessages(List<TwitchMessage> twitchMessages)
    {

            foreach (var twitchMessage in twitchMessages)
            {
                await AddMessage(twitchMessage);
            }

    }

    private void SendMessage()
    {
        Task.Run(async () =>
        {
            
            
            await streamWriter.WriteLineAsync($"PRIVMSG #{Channel} :{inputstring}");
            await AddMessage(new TwitchMessage(botUsername, inputstring));
            inputstring = string.Empty;
        });
        
    }

    #endregion

    #region events


    public string inputstring;
    private void KeyboardOnCharPres(IKeyboard arg1, char c)
    {

        inputstring += c;

    }
    private void WindowOnResize(Vector2D<int> obj)
    {
        WatingQueue.Wait();
        var renderTarget = new GRBackendRenderTarget(obj.X, obj.Y, 0, 8, new GRGlFramebufferInfo(0, 0x8058)); // 0x8058 = GL_RGBA8`
        SkSurface = SKSurface.Create(GrContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        SkCanvas = SkSurface.Canvas;
        MaxMessages = obj.Y / 19;
        Task.Run(async () => ReCalculateText());

        WatingQueue.Release();
    }


    public  void OnClose()
    {
        this.Dispose();
    }

    public abstract void OnRender(double obj);

    

    #endregion



    #region misc

    public void Dispose()
    {
        GrContext.Dispose();
        SkSurface.Dispose();
        SkCanvas.Dispose();
        
        if(streamWriter is not null) streamWriter.Dispose();
        if(streamReader is not null)streamReader.Dispose();
        if(tcpClient is not null)tcpClient.Dispose();
    }
    
    public async Task<(TwitchMessage text, SKPoint drawOrigin)> PrepareText(TwitchMessage text)
    {
        //bottom of the screen 
        var lastRect = new SKPoint(10,Window.Size.Y - 10);
        
        //Move Message up
        
        var TranformVal = (int)Math.Ceiling(18.5);

        Messages = Messages.Select(x =>
        {
            x.drawOrigin.Y -= TranformVal;
            return x;
        }).ToList();

        List<(TwitchMessage text, SKPoint drawOrigin)> vals = new();
        Debug.WriteLine($"Rect {lastRect}");
        var rect = new SKRect();
            MessagePen.MeasureText(text.ToString(), ref rect);
            
            //calculate where to place text

            
            var drawOrigin = new SKPoint
            (
                x:lastRect.X,
                y: lastRect.Y - (rect.Height + 10)
            );
            return (text,drawOrigin);
    }
    public async void ReCalculateText()
    {
        if (Messages.Count == 0)return;



        (TwitchMessage text, SKPoint drawOrigin)[] messages = new (TwitchMessage text, SKPoint drawOrigin)[Messages.Count];
        Messages.CopyTo(messages);
        Messages.Clear();
        
        await Task.Run(async () =>
        {
            var MessageBuilder = messages[0].text;
            foreach (var VARIABLE in messages.Skip(1))
            {
                if (VARIABLE.text.Nickname == MessageBuilder.Nickname)
                {
                    MessageBuilder.Message += $"{VARIABLE.text.Message} ";
                    continue;
                }
                await AddMessage(MessageBuilder);
                MessageBuilder = VARIABLE.text;
            }
            
            await AddMessage(MessageBuilder);
            
        });

    }

    #endregion

    #region TwitchConnection
    
     public string botUsername;


    public readonly SemaphoreSlim WatingQueue = new(1);
    public readonly SemaphoreSlim ConnectionQueue = new(1);
    public StreamWriter? streamWriter;
    public StreamReader? streamReader;
    public TcpClient? tcpClient = new TcpClient();
    public async void ReadFromIRC()
    {
        while (!tcpClient.Connected)
        {
            await Task.Delay(100);
        }
        while (true)
        {
            try
            {
                string line = await streamReader.ReadLineAsync();

                if (line is null) continue;
            
            
                //Keep Alive
                if (line.Contains("PING"))
                {
                    await streamWriter.WriteLineAsync("PONG :tmi.twitch.tv");
                    continue;
                }

                if (!line.Contains("PRIVMSG"))
                {
                    Debug.WriteLine(line);
                    continue;

                }

                //Parse Message
                var messages = Lexer.ParseMessage(line);


                await AddMessages(messages);
                

            }
            catch (Exception e)
            {
                await AddMessage(new ("System", "there was an error in connection try again"));
                tcpClient.Dispose();
                tcpClient = null;
                streamReader?.Dispose();
                streamReader = null;
                await streamWriter!.DisposeAsync();
                streamWriter = null;
                Channel = null;
                return;
            }
           
        }
        
        
    }
    #endregion


    public async Task ClearMessages()
    {
        Messages.Clear();
        
    }
}





