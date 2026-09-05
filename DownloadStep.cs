using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public abstract class DownloadStep
{
    public readonly VidDownloader Downloader;
    public DownloadStep(VidDownloader downloader)
    {
        Downloader = downloader;
    }

    protected abstract ReplyKeyboardMarkup CreateMarkup();
    protected abstract string CreateStepMessage();

    /// <summary>
    /// Check if the step should be skipped and return true if so.
    /// </summary>
    public abstract bool ShouldSkipStep();

    public async Task SendStepMessage()
    {
        await Downloader.Bot.SendMessage(Downloader.Chat, CreateStepMessage(), replyMarkup:CreateMarkup().AddNewRow().AddButton(new KeyboardButton("Cancel")));
    }

    /// <summary>
    /// Handle message sent by user and determine if step is finished or not. 
    /// When step is finished use <see cref="Downloader.NextStep()"/> to move to the next step.
    /// </summary>
    public abstract Task HandleMsg(Message msg);
}
