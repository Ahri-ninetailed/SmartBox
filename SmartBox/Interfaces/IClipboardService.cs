namespace SmartBox.Interfaces
{
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
        Task SetFileAsync(string filePath);
    }
}
