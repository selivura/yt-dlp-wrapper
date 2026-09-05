using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public partial class YtDlpBot
{
    private readonly TelegramBotClient _bot;
    private readonly User _botUser;

    private const string START_COMMAND = "/start";
    public readonly string[] YtLinkStarts = new[]
    {
        "https://youtube.com/v/",
        "https://youtu.be/",
        "https://www.youtube.com/watch?v="
    };

    private HashSet<VidDownloader> _vidDownloaders = new();

    public YtDlpBot(TelegramBotClient bot, User botUser)
    {
        _bot = bot;
        _botUser = botUser;   
    }

    public void Initialize()
    {
        _bot.OnMessage += OnMessage;   
        _bot.OnError += OnError;
    }

    public void Deinitialize()
    {
        _bot.OnError -= OnError;
        _bot.OnMessage -= OnMessage;
    }

    async Task OnError(Exception exception, HandleErrorSource source)
    {
        Console.WriteLine(exception); 
    }

    async Task OnMessage(Message msg, UpdateType type)
    {
        if (msg.Text is null) return;

        Console.WriteLine($"Received {type} '{msg.Text}' in {msg.Chat}");
        if(msg.Text == "/start")
        {
            await HandleStartMessage(msg, type);
            return;
        }
        
        foreach (var linkStart in YtLinkStarts)
        {
            if(msg.Text.StartsWith(linkStart))
            {
                await HandleYTLinkMessage(msg, linkStart);
                return;
            }
        }
        
        await _bot.SendMessage(msg.Chat, $"Invalid link.");
    }

    async Task HandleStartMessage(Message msg, UpdateType type)
    {
        await _bot.SendMessage(msg.Chat, $"YT-DLP bot ready. Send yt link.");
    }

    async Task HandleYTLinkMessage(Message msg, string linkStart)
    {
        var ytVidId = msg.Text.Remove(0, linkStart.Length);

        if(ytVidId.Length != 11)
        {
            await _bot.SendMessage(msg.Chat, $"{msg.Text} is invalid");
        }
        var vidDownloader = new VidDownloader(msg.Chat, ytVidId, _bot);
        _vidDownloaders.Add(vidDownloader);
        await vidDownloader.Start();
    }
}
