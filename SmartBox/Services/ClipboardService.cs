using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class ClipboardService : IClipboardService
    {
        public async Task SetTextAsync(string text)
        {
            await Clipboard.Default.SetTextAsync(text);
        }

        public async Task SetFileAsync(string filePath)
        {
#if WINDOWS
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetStorageItems(new[] { file });
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
#else
            await Clipboard.Default.SetTextAsync(filePath);
#endif
        }
    }
}
