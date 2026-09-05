using System.Diagnostics;
using System.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Types;

public partial class YtDlpBot
{
    private class VidDownloader
    {
        private const string YT_DLP_PATH = "yt-dlp";
        private readonly Chat _chat;
        private readonly string _vidId;
        private readonly TelegramBotClient _bot;
        private string _vidPath => $"users/{_chat.Id}/{_vidId}.mp4";

        public VidDownloader(Chat chat, string vidId, TelegramBotClient bot)
        {
            _chat = chat;
            _vidId = vidId;
            _bot = bot;
        }

        public async Task Start()
        {
            await _bot.SendMessage(_chat, $"Starting download for {_vidId}");

            Directory.CreateDirectory(_chat.Id.ToString());

            Process process = new Process();
            process.StartInfo.FileName = YT_DLP_PATH;
            process.StartInfo.ArgumentList.Add("-f");
            process.StartInfo.ArgumentList.Add("best[ext=mp4]/best");

            process.StartInfo.ArgumentList.Add("-o");
            process.StartInfo.ArgumentList.Add(_vidPath);
            process.StartInfo.ArgumentList.Add(_vidId);

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();

            string stdOut = await process.StandardOutput.ReadToEndAsync();
            string stdErr = await process.StandardError.ReadToEndAsync();

            Console.WriteLine(stdOut);

            await process.WaitForExitAsync();

            if(process.ExitCode != 0)
            {
                throw new Exception($"yt-dlp failed:\n{stdErr}");
            }

            if(!File.Exists(_vidPath))
            {
                throw new FileNotFoundException($"yt-dlp did not produce a file.");
            }

            var httpClient = new HttpClient();
            var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

            string tgUrl = $"https://api.telegram.org/bot{botToken}/sendDocument";

            var form = new MultipartFormDataContent();
            form.Add(new StringContent(_chat.Id.ToString()), "chat_id");

            var fileStream = File.OpenRead(_vidPath);
            
            var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");

            form.Add(fileContent, "document", Path.GetFileName(_vidPath));
            HttpResponseMessage response = await httpClient.PostAsync(tgUrl, form);

            string tgResponse = await response.Content.ReadAsStringAsync();

            if(!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Telegram upload failed: {response.StatusCode}\n{tgResponse}");
            }

            Console.WriteLine($"{_chat.Id}: File sent.\n{tgResponse}");

            //File.Delete(_vidPath);
        }
    }
}
