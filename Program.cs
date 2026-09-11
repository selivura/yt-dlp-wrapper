using Telegram.Bot;

namespace yt_dlp_wrapper;

public static class Program
{
    public static async Task Main()
    {
        var envPath = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env")
        }.FirstOrDefault(File.Exists);

        if (envPath is null)
        {
            Console.WriteLine("Add .env file to the working directory or executable directory.");
            return;
        }

        var values = LoadEnvFile(envPath);

        if (!values.TryGetValue("BOT_TOKEN", out var botToken) || string.IsNullOrWhiteSpace(botToken))
        {
            Console.WriteLine("The .env file does not contain BOT_TOKEN.");
            return;
        }

        Environment.SetEnvironmentVariable("BOT_TOKEN", botToken.Trim());

        foreach (var key in new[] { "FFMPEG_PATH", "YT-DLP_PATH", "YT_DLP_PATH" })
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                var normalizedValue = ResolveConfiguredPath(value.Trim(), envPath);
                Environment.SetEnvironmentVariable(key, normalizedValue);
            }
        }

        using var cts = new CancellationTokenSource();

        TelegramBotClient bot;
        try
        {
            bot = new TelegramBotClient(botToken.Trim(), cancellationToken: cts.Token);
        }
        catch
        {
            throw new ArgumentException("Invalid token.");
        }

        Console.WriteLine("Awaiting botUser from Telegram...");
        var botUser = await bot.GetMe();

        var ytDlpBot = new YtDlpBot(bot, botUser);
        await ytDlpBot.Initialize();

        Console.WriteLine("Bot is running. Press Enter to stop.");
        Console.ReadLine();

        await ytDlpBot.Deinitialize();
        cts.Cancel();
    }

    private static Dictionary<string, string> LoadEnvFile(string envPath)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 && ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            values[key] = value;
        }

        return values;
    }

    private static string ResolveConfiguredPath(string value, string envPath)
    {
        var trimmed = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        if (!Path.IsPathRooted(trimmed))
        {
            var baseDirectory = Path.GetDirectoryName(envPath) ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(baseDirectory, trimmed));
        }

        return Path.GetFullPath(trimmed);
    }
}
