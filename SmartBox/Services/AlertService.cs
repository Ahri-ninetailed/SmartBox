using Microsoft.Extensions.Logging;
using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class AlertService : IAlertService
    {
        private readonly ILogger<AlertService> _logger;

        public AlertService(ILogger<AlertService> logger)
        {
            _logger = logger;
        }

        public async Task DisplayAlertAsync(string message, string title, string cancel)
        {
            _logger.LogTrace("DisplayAlertAsync started - Thread: {ThreadId}, Message: '{Message}', Title: '{Title}', Cancel: '{Cancel}'",
                Thread.CurrentThread.ManagedThreadId, message, title, cancel);

            try
            {
                _logger.LogTrace("Checking if on main thread - Thread: {ThreadId}, IsMainThread: {IsMainThread}",
                    Thread.CurrentThread.ManagedThreadId, MainThread.IsMainThread);

                if (MainThread.IsMainThread)
                {
                    _logger.LogTrace("On main thread, checking MainPage availability - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                    if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                    {
                        _logger.LogTrace("MainPage available, calling DisplayAlert directly - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                        await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, cancel);
                        _logger.LogTrace("DisplayAlert completed successfully - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    }
                    else
                    {
                        _logger.LogTrace("MainPage is null, cannot display alert - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    }
                }
                else
                {
                    _logger.LogTrace("Not on main thread, invoking on main thread - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        _logger.LogTrace("Main thread action started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                        if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
                        {
                            _logger.LogTrace("MainPage available in main thread action, calling DisplayAlert - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                            await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, cancel);
                            _logger.LogTrace("DisplayAlert completed in main thread action - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                        }
                        else
                        {
                            _logger.LogTrace("MainPage is null in main thread action - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                        }

                        _logger.LogTrace("Main thread action completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    });

                    _logger.LogTrace("InvokeOnMainThreadAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisplayAlertAsync failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger.LogTrace("DisplayAlertAsync completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}