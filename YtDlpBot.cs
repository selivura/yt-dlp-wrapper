using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace yt_dlp_wrapper;

public class YtDlpBot
{
    private const string StartCommand = "/start";

    private readonly TelegramBotClient _bot;
    private readonly User _botUser;
    private readonly Dictionary<long, DownloadSession> _sessions = new();

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

    private Task OnError(Exception exception, HandleErrorSource source)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }

    private async Task OnMessage(Message msg, UpdateType type)
    {
        if (msg.Text is null)
        {
            return;
        }

        Console.WriteLine($"Received {type} '{msg.Text}' in {msg.Chat}");

        if (_sessions.TryGetValue(msg.Chat.Id, out var activeSession))
        {
            if (activeSession.IsFinished)
            {
                _sessions.Remove(msg.Chat.Id);
            }
            else
            {
                await activeSession.HandleMessageAsync(msg);
                return;
            }
        }

        if (msg.Text.Equals(StartCommand, StringComparison.OrdinalIgnoreCase))
        {
            await HandleStartMessage(msg);
            return;
        }

        if (YoutubeLinkParser.TryParseVideoId(msg.Text, out var videoId))
        {
            await StartDownloadSession(msg.Chat, videoId);
            return;
        }

        await _bot.SendMessage(msg.Chat, "Invalid link.");
    }

    private async Task HandleStartMessage(Message msg)
    {
        await _bot.SendMessage(msg.Chat, "YT-DLP bot ready. Send YT link.");
    }

    private async Task StartDownloadSession(Chat chat, string videoId)
    {
        var session = new DownloadSession(chat, videoId, _bot);
        _sessions[chat.Id] = session;

        Console.WriteLine($"Added new downloader {chat.Id}");
        await session.StartAsync();
    }
}