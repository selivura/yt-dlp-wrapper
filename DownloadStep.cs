using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace yt_dlp_wrapper;

public abstract class DownloadStep
{
    protected DownloadSession Session { get; }

    protected DownloadStep(DownloadSession session)
    {
        Session = session;
    }

    protected abstract ReplyKeyboardMarkup CreateMarkup();
    protected abstract string CreateStepMessage();

    public abstract bool ShouldSkipStep();

    public async Task SendStepMessageAsync()
    {
        var rows = CreateMarkup().Keyboard
            .Select(row => row.ToList())
            .ToList();

        rows.Add(new List<KeyboardButton>
        {
            new("Cancel")
        });

        var keyboard = new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await Session.Notifier.SendMessageAsync(Session.Chat, CreateStepMessage(), keyboard);
    }

    public abstract Task HandleMessageAsync(Message msg);
}
