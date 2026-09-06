using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class VidDownloader
{
    private const string YT_DLP_PATH = "yt-dlp"; 
    public readonly Chat Chat;
    public readonly TelegramBotClient Bot;
    public readonly string VidId;
    private string _vidPath => $"user-cache/{Chat.Id}/";
    private List<string> _arguments = new();

    private List<DownloadStep> _downloadSteps;
    private int _currentStepIndex = 0;

    public bool Finished { get; private set; } = false;
    private bool _downloadingNow = false;

    public void AddYtDlpArg(string argument)
    {
        _arguments.Add(argument);
    }

    public VidDownloader(Chat chat, string vidId, TelegramBotClient bot)
    {
        Chat = chat;
        VidId = vidId;
        Bot = bot;

        //ADD DOWNLOAD STEPS HERE:
        _downloadSteps = 
        [
            new VideoOrAudioOnlyStep(this),
        ];
    }

    public async Task Download()
    {
        await Bot.SendMessage(Chat, $"Starting download for {VidId}");
        _downloadingNow = true;

        Process process = new Process();
        process.StartInfo.FileName = YT_DLP_PATH;
        
        foreach (var arg in _arguments) //Add all arguments
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add($"{_vidPath}{VidId}.%(ext)s");
        process.StartInfo.ArgumentList.Add(VidId);

        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();

        string stdOut = await process.StandardOutput.ReadToEndAsync();
        string stdErr = await process.StandardError.ReadToEndAsync();

        Console.WriteLine(stdOut);

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"yt-dlp failed:\n{stdErr}");
        }

        string? filePath = Directory
            .GetFiles(_vidPath)
            .FirstOrDefault(file => Path.GetFileName(file).StartsWith(VidId, StringComparison.Ordinal));

        if (filePath is null)
        {
            throw new FileNotFoundException($"yt-dlp did not produce a file.");
        }

        var fileStream = File.OpenRead(filePath);

        await Bot.SendDocument(Chat, fileStream);

        File.Delete(filePath);

        Finished = true;
        Console.WriteLine($"{Chat.Id}: Download finished.");
    }

    public async Task BeginDownloadSetup()
    {
        await _downloadSteps[_currentStepIndex].SendStepMessage();
    }
    public async Task HandleMsg(Message msg)
    {
        if(msg.Text is null) return;
        if(_downloadingNow)
        {
            await Bot.SendMessage(Chat, "Video is currently being downloaded, please wait for it to finish.");
            return;
        }
        if(msg.Text == "Cancel")
        {
            await Bot.SendMessage(Chat, "Donwload canelled. Send another link to begin new donwload.", replyMarkup:new ReplyKeyboardRemove());
            Finished = true;
            return;
        }

        await _downloadSteps[_currentStepIndex].HandleMsg(msg);
    }

    /// <summary>
    /// Use this to finish current step and continue to the next one.
    /// </summary>
    public async Task NextStep()
    {
        _currentStepIndex++;
        if(_downloadSteps.Count <= _currentStepIndex)
        {
            await FinishDownloadSetup();
            return;
        }
        if(_downloadSteps[_currentStepIndex].ShouldSkipStep())
        {
            await NextStep();
            return;
        }
        await _downloadSteps[_currentStepIndex].SendStepMessage();
    }
    
    /// <summary>
    /// Use this when download is ready.
    /// </summary>
    public async Task FinishDownloadSetup()
    {
        try
        {
            await Download();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{Chat.Id}: Download/upload error.\n {ex.Message}");
            await Bot.SendMessage(Chat, "An error occured during download/upload. Try again.");
        }
    }
}