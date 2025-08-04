using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class AlertService : IAlertService
    {
        public Task DisplayAlertAsync(string message, string title, string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
                Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert(title, message, cancel)
            );
        }
    }
}
