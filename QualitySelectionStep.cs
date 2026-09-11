using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace yt_dlp_wrapper;

public class QualitySelectionStep : DownloadStep
{
    public QualitySelectionStep(DownloadSession session) : base(session)
    {
    }

    public override async Task HandleMessageAsync(Message msg)
    {
        var text = msg.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            await SendStepMessageAsync();
            return;
        }

        var selection = text.Trim();
        Session.Quality = selection;

        Console.WriteLine($"{Session.Chat.Id}: Selected quality '{selection}'");

        await Session.Notifier.SendSelectionSavedAsync(Session.Chat);
        await Session.NextStepAsync();
    }

    public override bool ShouldSkipStep()
    {
        return false;
    }

    protected override ReplyKeyboardMarkup CreateMarkup()
    {
        var rows = new List<List<KeyboardButton>>();

        if (Session.IsAudioOnly)
        {
            rows.Add(new List<KeyboardButton> { new("320k"), new("192k") });
            rows.Add(new List<KeyboardButton> { new("128k"), new("best") });
        }
        else
        {
            rows.Add(new List<KeyboardButton> { new("1080"), new("720") });
            rows.Add(new List<KeyboardButton> { new("480"), new("best") });
        }

        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    protected override string CreateStepMessage()
    {
        return Session.IsAudioOnly ? "Choose audio quality (kbps or 'best'):" : "Choose maximum video height (e.g. 720) or 'best':";
    }
}
