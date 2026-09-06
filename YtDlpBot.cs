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


    private List<VidDownloader> _vidDownloaders = new();

    public YtDlpBot(TelegramBotClient bot, User botUser)
    {
        _bot = bot;
        _botUser = botUser;   
    }

    public async Task Initialize()
    {
        _bot.OnMessage += OnMessage;   
        _bot.OnError += OnError;
        Console.WriteLine("Bot initialized.");
    }

    public async Task Deinitialize()
    {
        _bot.OnError -= OnError;
        _bot.OnMessage -= OnMessage;
        Console.WriteLine("Bot stopped.");
    }

    async Task OnError(Exception exception, HandleErrorSource source)
    {
        Console.WriteLine(exception); 
    }

    async Task OnMessage(Message msg, UpdateType type)
    {
        if (msg.Text is null) return;

        Console.WriteLine($"Received {type} '{msg.Text}' in {msg.Chat}");
        
        for (int i = _vidDownloaders.Count - 1; i >= 0; i--)
        {
            if(msg.Chat.Id == _vidDownloaders[i].Chat.Id)
            {
                if(_vidDownloaders[i].Finished) // Remove finished downloads from list.
                {
                    _vidDownloaders.RemoveAt(i);
                    break;
                }

                await _vidDownloaders[i].HandleMsg(msg);
                return;
            }
        }

        if(msg.Text == START_COMMAND)
        {
            await HandleStartMessage(msg);
            return;
        }
        
        foreach (var linkStart in YtLinkStarts)
        {
            if(msg.Text.StartsWith(linkStart))
            {
                await HandleLinkMessage(msg, linkStart);
                return;
            }
        }
        
        await _bot.SendMessage(msg.Chat, $"Invalid link.");
    }

    async Task HandleStartMessage(Message msg)
    {
        await _bot.SendMessage(msg.Chat, $"YT-DLP bot ready. Send YT link.");
    }

    async Task HandleLinkMessage(Message msg, string linkStart)
    {
        var ytVidId = msg.Text.Remove(0, linkStart.Length);

        if(ytVidId.Length != 11)
        {
            await _bot.SendMessage(msg.Chat, $"{msg.Text} is invalid");
        }
        var vidDownloader = new VidDownloader(msg.Chat, ytVidId, _bot);

        _vidDownloaders.Add(vidDownloader);
        Console.WriteLine($"Added new downloader {msg.Chat.Id}");
        await vidDownloader.BeginDownloadSetup();
    }
}