using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace yt_dlp_wrapper;

public class TelegramNotifier
{
    private readonly TelegramBotClient _bot;

    public TelegramBotClient Bot => _bot;

    public TelegramNotifier(TelegramBotClient bot)
    {
        _bot = bot;
    }

    public Task SendMessageAsync(Chat chat, string text, ReplyMarkup? replyMarkup = null)
    {
        return _bot.SendMessage(chat, text, replyMarkup: replyMarkup);
    }

    public Task SendDocumentAsync(Chat chat, Stream fileStream)
    {
        return _bot.SendDocument(chat, fileStream);
    }

    public Task SendSelectionSavedAsync(Chat chat)
    {
        return _bot.SendMessage(chat, "Selection saved.", replyMarkup: new ReplyKeyboardRemove());
    }

    public Task SendDownloadStartedAsync(Chat chat, string videoId)
    {
        return _bot.SendMessage(chat, $"Starting download for {videoId}");
    }

    public Task SendDownloadInProgressAsync(Chat chat)
    {
        return _bot.SendMessage(chat, "Video is currently being downloaded, please wait for it to finish.");
    }

    public Task SendCancelledAsync(Chat chat)
    {
        return _bot.SendMessage(chat, "Download cancelled. Send another link to begin a new download.", replyMarkup: new ReplyKeyboardRemove());
    }

    public Task SendErrorAsync(Chat chat, string message)
    {
        return _bot.SendMessage(chat, message);
    }
}
