using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class AlertService : IAlertService
    {
        public async Task DisplayAlertAsync(string message, string title, string cancel)
        {
            if (MainThread.IsMainThread)
            {
                if (Microsoft.Maui.Controls.Application.Current?.Windows[0]?.Page != null)
                {
                    await Microsoft.Maui.Controls.Application.Current.Windows[0].Page!.DisplayAlert(title, message, cancel);
                }
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Microsoft.Maui.Controls.Application.Current?.Windows[0]?.Page != null)
                    {
                        await Microsoft.Maui.Controls.Application.Current.Windows[0].Page!.DisplayAlert(title, message, cancel);
                    }
                });
            }
        }
    }
}
