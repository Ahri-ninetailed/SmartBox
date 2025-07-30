using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class AlertService : IAlertService
    {
        public Task DisplayAlertAsync(string title, string message, string cancel = "Ok")
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
                Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert(title, message, cancel)
            );
        }
    }
}
