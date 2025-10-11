namespace SmartBox.Behaviors
{
    public class SelectAllOnFocusBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry bindable)
        {
            bindable.Focused += OnEntryFocused;
            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(Entry bindable)
        {
            bindable.Focused -= OnEntryFocused;
            base.OnDetachingFrom(bindable);
        }

        private void OnEntryFocused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry && !string.IsNullOrEmpty(entry.Text))
            {
                entry.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
                {
                    entry.CursorPosition = 0;
                    entry.SelectionLength = entry.Text.Length;
                });
            }
        }
    }
}
