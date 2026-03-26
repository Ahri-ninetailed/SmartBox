using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class AlertService : IAlertService
    {
        public async Task DisplayAlertAsync(string message, string title, string cancel)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
                {
                    await page.DisplayAlert(title, message, cancel);
                }
            });
        }
    }
}
