using System.Diagnostics;
using System.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class VidDownloader
{
    private const string YT_DLP_PATH = "yt-dlp";
    public readonly Chat Chat;
    public readonly TelegramBotClient Bot;
    public readonly string VidId;
    private string _vidPath => $"users/{Chat.Id}/{VidId}.{FileExt}";
    private List<string> _arguments = new();

    private List<DownloadStep> _downloadSteps;
    private int _currentStepIndex = 0;

    public bool Finished { get; private set; } = false;
    private bool _downloadingNow = false;

    /// <summary>
    /// Set this to appropriate filex extension of the download
    /// </summary>
    public string FileExt = "";


    /// <summary>
    /// Set this to appropriate filex extension of the download
    /// application/octet-stream by default
    /// </summary>
    public string MIME = "application/octet-stream";

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

        Directory.CreateDirectory(Chat.Id.ToString());

        Process process = new Process();
        process.StartInfo.FileName = YT_DLP_PATH;
        
        foreach (var arg in _arguments) //Add all arguments
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add(_vidPath);
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

        if (!File.Exists(_vidPath))
        {
            throw new FileNotFoundException($"yt-dlp did not produce a file.");
        }

        var httpClient = new HttpClient();
        var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

        string tgUrl = $"https://api.telegram.org/bot{botToken}/sendDocument";

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(Chat.Id.ToString()), "chat_id");

        var fileStream = File.OpenRead(_vidPath);

        var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MIME);

        form.Add(fileContent, "document", Path.GetFileName(_vidPath));
        HttpResponseMessage response = await httpClient.PostAsync(tgUrl, form);

        string tgResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Telegram upload failed: {response.StatusCode}\n{tgResponse}");
        }

        Console.WriteLine($"{Chat.Id}: File sent.\n{tgResponse}");
        File.Delete(_vidPath);

        Finished = true;
    }

    public async Task BeginDownloadSetup()
    {
        await _downloadSteps[_currentStepIndex].SendStepMessage();
    }
    public async Task HandleMsg(Message msg)
    {
        if(msg.Text is null) return;
        Console.WriteLine($"Downloader {Chat.Id} recieved message: {msg.Text}");
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
        await Bot.SendMessage(Chat, "Starting download...");
        await Download();
    }
}