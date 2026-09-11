using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace yt_dlp_wrapper;

public class MediaTypeStep : DownloadStep
{
    private const string Video = "Video";
    private const string Audio = "Audio";

    public MediaTypeStep(DownloadSession session) : base(session)
    {
    }

    public override async Task HandleMessageAsync(Message msg)
    {
        var text = msg.Text ?? string.Empty;

        switch (text)
        {
            case Video:
                Console.WriteLine($"{Session.Chat.Id}: Selected Video");
                Session.IsAudioOnly = false;
                await CompleteStep();
                return;
            case Audio:
                Console.WriteLine($"{Session.Chat.Id}: Selected Audio");
                Session.IsAudioOnly = true;
                await CompleteStep();
                return;
            default:
                await SendStepMessageAsync();
                return;
        }
    }

    private async Task CompleteStep()
    {
        await Session.Notifier.SendSelectionSavedAsync(Session.Chat);
        await Session.NextStepAsync();
    }

    public override bool ShouldSkipStep()
    {
        return false;
    }

    protected override ReplyKeyboardMarkup CreateMarkup()
    {
        var keyboard = new List<List<KeyboardButton>>
        {
            new() { new KeyboardButton(Video) },
            new() { new KeyboardButton(Audio) }
        };

        return new ReplyKeyboardMarkup(keyboard)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    protected override string CreateStepMessage()
    {
        return "Choose media type:";
    }
}