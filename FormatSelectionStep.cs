using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace yt_dlp_wrapper;

public class FormatSelectionStep : DownloadStep
{
    public FormatSelectionStep(DownloadSession session) : base(session)
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

        // Normalize selection
        var selection = text.Trim();
        Session.Format = selection;

        Console.WriteLine($"{Session.Chat.Id}: Selected format '{selection}'");

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
            rows.Add(new List<KeyboardButton> { new("mp3"), new("m4a") });
            rows.Add(new List<KeyboardButton> { new("aac"), new("opus") });
            rows.Add(new List<KeyboardButton> { new("default") });
        }
        else
        {
            rows.Add(new List<KeyboardButton> { new("mp4"), new("mkv") });
            rows.Add(new List<KeyboardButton> { new("webm"), new("default") });
        }

        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    protected override string CreateStepMessage()
    {
        return Session.IsAudioOnly ? "Choose audio format (or 'default'):" : "Choose video format (or 'default'):";
    }
}
