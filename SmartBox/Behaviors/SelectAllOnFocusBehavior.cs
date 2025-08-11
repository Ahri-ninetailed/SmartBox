using Microsoft.Extensions.Logging;

namespace SmartBox.Behaviors
{
    public class SelectAllOnFocusBehavior : Behavior<Entry>
    {
        private ILogger<SelectAllOnFocusBehavior> _logger = IPlatformApplication.Current?.Services?.GetService<ILogger<SelectAllOnFocusBehavior>>()!;

        protected override void OnAttachedTo(Entry bindable)
        {
            _logger?.LogTrace("OnAttachedTo started - Thread: {ThreadId}, Entry: {EntryType}",
                Thread.CurrentThread.ManagedThreadId, bindable?.GetType().Name ?? "null");

            try
            {
                _logger?.LogTrace("Subscribing to Focused event - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                bindable.Focused += OnEntryFocused;

                _logger?.LogTrace("Calling base.OnAttachedTo - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                base.OnAttachedTo(bindable);
                _logger?.LogTrace("base.OnAttachedTo completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "OnAttachedTo failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger?.LogTrace("OnAttachedTo completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected override void OnDetachingFrom(Entry bindable)
        {
            _logger?.LogTrace("OnDetachingFrom started - Thread: {ThreadId}, Entry: {EntryType}",
                Thread.CurrentThread.ManagedThreadId, bindable?.GetType().Name ?? "null");

            try
            {
                _logger?.LogTrace("Unsubscribing from Focused event - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                bindable.Focused -= OnEntryFocused;

                _logger?.LogTrace("Calling base.OnDetachingFrom - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                base.OnDetachingFrom(bindable);
                _logger?.LogTrace("base.OnDetachingFrom completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "OnDetachingFrom failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger?.LogTrace("OnDetachingFrom completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        private void OnEntryFocused(object? sender, FocusEventArgs e)
        {
            _logger?.LogTrace("OnEntryFocused started - Thread: {ThreadId}, IsFocused: {IsFocused}",
                Thread.CurrentThread.ManagedThreadId, e.IsFocused);

            try
            {
                var entry = (Entry)sender!;
                _logger?.LogTrace("Entry cast completed, Text: '{Text}', Length: {Length} - Thread: {ThreadId}",
                    entry.Text ?? "null", entry.Text?.Length ?? 0, Thread.CurrentThread.ManagedThreadId);

                if (!string.IsNullOrEmpty(entry.Text))
                {
                    _logger?.LogTrace("Text is not empty, getting dispatcher - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                    var dispatcher = Microsoft.Maui.Dispatching.Dispatcher.GetForCurrentThread();
                    if (dispatcher != null)
                    {
                        _logger?.LogTrace("Dispatcher available, scheduling delayed action - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                        dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1), () =>
                        {
                            _logger?.LogTrace("Delayed action started - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);

                            try
                            {
                                _logger?.LogTrace("Setting cursor position to 0 - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                                entry.CursorPosition = 0;

                                _logger?.LogTrace("Setting selection length to {Length} - Thread: {ThreadId}",
                                    entry.Text.Length, Thread.CurrentThread.ManagedThreadId);
                                entry.SelectionLength = entry.Text.Length;

                                _logger?.LogTrace("Text selection completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Delayed action failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                            }
                            finally
                            {
                                _logger?.LogTrace("Delayed action completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                            }
                        });

                        _logger?.LogTrace("DispatchDelayed call completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    }
                    else
                    {
                        _logger?.LogTrace("Dispatcher is null, cannot schedule delayed action - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    }
                }
                else
                {
                    _logger?.LogTrace("Text is null or empty, skipping selection - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "OnEntryFocused failed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                throw;
            }
            finally
            {
                _logger?.LogTrace("OnEntryFocused completed - Thread: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}