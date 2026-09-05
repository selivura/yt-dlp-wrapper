using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class VideoOrAudioOnlyStep : DownloadStep
{
    const string ALL = "Video & audio";
    const string VIDEO = "Video only";
    const string AUDIO = "Audio only";

    public VideoOrAudioOnlyStep(VidDownloader downloader) : base(downloader)
    {
    }

    public override async Task HandleMsg(Message msg)
    {
        Console.WriteLine($"VideoOrAudioOnlyStep {Downloader.Chat.Id} recieved message: {msg.Text}");
        bool success = false;
        switch (msg.Text)
        {
            case ALL:
                {
                    Console.WriteLine($"{Downloader.Chat.Id}: Set best");
                    Downloader.AddYtDlpArg("-f");
                    Downloader.AddYtDlpArg("best");
                    success = true;
                    break;
                }
            case AUDIO:
                {
                    Console.WriteLine($"{Downloader.Chat.Id}: Set bestaudio");
                    Downloader.AddYtDlpArg("-f");
                    Downloader.AddYtDlpArg("bestaudio");
                    success = true;
                    break;
                }
            case VIDEO:
                {
                    Console.WriteLine($"{Downloader.Chat.Id}: Set bestvideo");
                    Downloader.AddYtDlpArg("-f");
                    Downloader.AddYtDlpArg("bestvideo");
                    success = true;
                    break;
                }
            default:
                {
                    await SendStepMessage();
                    break;
                }
        }
        if(success)
        {
            await Downloader.Bot.SendMessage(Downloader.Chat, "Selection saved.", replyMarkup:new ReplyKeyboardRemove());
            await Downloader.NextStep();
        }
    }

    public override bool ShouldSkipStep()
    {
        return false;
    }

    protected override ReplyKeyboardMarkup CreateMarkup()
    {
        var keyboard = new List<List<KeyboardButton>>
        {
            new()
            {
                new KeyboardButton(ALL)
            },
            new()
            {
                new KeyboardButton(VIDEO),
                new KeyboardButton(AUDIO)
            }
        };

        return new ReplyKeyboardMarkup(keyboard)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    protected override string CreateStepMessage()
    {
        return "Video, video only or audio only?";
    }
}