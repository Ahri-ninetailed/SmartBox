namespace SmartBox.Interfaces
{
    public interface IPromptService
    {
        Task<string> ShowPromptAsync(string title, string message, string initialValue);
    }
}
