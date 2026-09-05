using System;
using Telegram.Bot;

public class Program
{
    private static async Task Main(string[] args)
    {
        if(!File.Exists("token.env"))
        {
            Console.WriteLine("Add token.env file to executble directiory.");
            return;
        }

        string botToken = File.ReadLines("token.env").First();
        Environment.SetEnvironmentVariable("BOT_TOKEN", botToken);
        
        using var cts = new CancellationTokenSource();
        TelegramBotClient bot;
        try
        {
            bot = new TelegramBotClient(botToken, cancellationToken: cts.Token);   
        }
        catch
        {
            throw new ArgumentException("Invalid token");
        }

        Console.WriteLine("Awaiting botUser from telegram...");

        var botUser = await bot.GetMe();

        YtDlpBot ytdlpBot = new (bot, botUser);

        await ytdlpBot.Initialize();

        Console.ReadLine();

        await ytdlpBot.Deinitialize();
        cts.Cancel();
    }
}
