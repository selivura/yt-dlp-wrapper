using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace yt_dlp_wrapper;

public class DownloadSession
{
    private readonly TelegramNotifier _notifier;
    private readonly List<DownloadStep> _steps;
    private readonly List<string> _downloadArguments = new();
    private int _currentStepIndex;

    public Chat Chat { get; }
    public string VideoId { get; }
    public bool IsAudioOnly { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public bool IsFinished { get; private set; }
    public bool IsDownloading { get; private set; }

    private string DownloadDirectory => Path.Combine("user-cache", Chat.Id.ToString());

    public DownloadSession(Chat chat, string videoId, TelegramBotClient bot)
    {
        Chat = chat;
        VideoId = videoId;
        _notifier = new TelegramNotifier(bot);
        _steps = new List<DownloadStep>
        {
            new MediaTypeStep(this),
            new FormatSelectionStep(this),
            new QualitySelectionStep(this)
        };
    }

    public TelegramNotifier Notifier => _notifier;

    public void AddYtDlpArg(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return;
        }

        _downloadArguments.Add(argument);
    }

    public async Task StartAsync()
    {
        await _steps[_currentStepIndex].SendStepMessageAsync();
    }

    public async Task HandleMessageAsync(Message msg)
    {
        if (msg.Text is null)
        {
            return;
        }

        if (IsDownloading)
        {
            await _notifier.SendDownloadInProgressAsync(Chat);
            return;
        }

        if (msg.Text.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
        {
            await _notifier.SendCancelledAsync(Chat);
            IsFinished = true;
            return;
        }

        await _steps[_currentStepIndex].HandleMessageAsync(msg);
    }

    public async Task NextStepAsync()
    {
        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Count)
        {
            await FinishSetupAsync();
            return;
        }

        if (_steps[_currentStepIndex].ShouldSkipStep())
        {
            await NextStepAsync();
            return;
        }

        await _steps[_currentStepIndex].SendStepMessageAsync();
    }

    public async Task FinishSetupAsync()
    {
        try
        {
            await DownloadAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Chat.Id}: Download/upload error.\n{ex.Message}");
            await _notifier.SendErrorAsync(Chat, "An error occurred during download/upload. Try again.");
            IsFinished = true;
        }
    }

    public async Task DownloadAsync()
    {
        Directory.CreateDirectory(DownloadDirectory);

        await _notifier.SendDownloadStartedAsync(Chat, VideoId);
        IsDownloading = true;

        try
        {
            var args = BuildYtDlpArgs();
            var filePath = await global::yt_dlp_wrapper.YtDlpRunner.RunAsync(VideoId, DownloadDirectory, args);

            await using var fileStream = File.OpenRead(filePath);
            await _notifier.SendDocumentAsync(Chat, fileStream);
            File.Delete(filePath);
        }
        finally
        {
            IsDownloading = false;
            IsFinished = true;
            Console.WriteLine($"{Chat.Id}: Download finished.");
        }
    }

    public List<string> BuildYtDlpArgs()
    {
        var args = new List<string>(_downloadArguments);

        if (IsAudioOnly)
        {
            args.Add("-x");
            if (!string.IsNullOrWhiteSpace(Format) && !Format.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("--audio-format");
                args.Add(Format);
            }

            if (!string.IsNullOrWhiteSpace(Quality) && !Quality.Equals("best", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("--audio-quality");
                args.Add(Quality);
            }
        }
        else
        {
            // Video
            if (!string.IsNullOrWhiteSpace(Quality) && !Quality.Equals("best", StringComparison.OrdinalIgnoreCase) && int.TryParse(Quality, out var height))
            {
                args.Add("-f");
                args.Add($"bestvideo[height<={height}]+bestaudio/best");
            }

            if (!string.IsNullOrWhiteSpace(Format) && !Format.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                args.Add("--recode-video");
                args.Add(Format);
            }

            if (!args.Any(a => a == "-f"))
            {
                args.Add("-f");
                args.Add("best");
            }
        }

        return args;
    }
}
