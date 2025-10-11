using System.Diagnostics;
using Syncfusion.Maui.TreeView;
using SmartBox.Models.Parser;

namespace SmartBox.Behaviors;

public class EditNodeOnTapBehavior : Behavior<SfTreeView>
{
    public static readonly BindableProperty IgnoreIsEnabledProperty =
        BindableProperty.Create(nameof(IgnoreIsEnabled), typeof(bool), typeof(EditNodeOnTapBehavior), false);

    public bool IgnoreIsEnabled
    {
        get => (bool)GetValue(IgnoreIsEnabledProperty);
        set => SetValue(IgnoreIsEnabledProperty, value);
    }

    private SfTreeView? _tree;

    protected override void OnAttachedTo(SfTreeView bindable)
    {
        base.OnAttachedTo(bindable);
        _tree = bindable;
        bindable.ItemTapped += OnItemTapped;
        Debug.WriteLine("[EditNodeOnTapBehavior] Attached");
    }

    protected override void OnDetachingFrom(SfTreeView bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.ItemTapped -= OnItemTapped;
        _tree = null;
        Debug.WriteLine("[EditNodeOnTapBehavior] Detached");
    }

    private async void OnItemTapped(object? sender, Syncfusion.Maui.TreeView.ItemTappedEventArgs e)
    {
        var content = e.Node?.Content;
        switch (content)
        {
            case Meaning meaning when (IgnoreIsEnabled || meaning.IsEnabled):
                await EditMeaningAsync(meaning);
                break;
            case Example example when (IgnoreIsEnabled || example.IsEnabled):
                await EditExampleAsync(example);
                break;
        }
    }

    private async Task EditMeaningAsync(Meaning meaning)
    {
        string result = await Microsoft.Maui.Controls.Application.Current!.Windows[0].Page!.DisplayPromptAsync(
            "Edit Meaning", "Enter new meaning:", initialValue: meaning.Value, maxLength: 500, keyboard: Keyboard.Default);

        if (!string.IsNullOrWhiteSpace(result) && result != meaning.Value)
        {
            meaning.Value = result.Trim();
        }
    }

    private async Task EditExampleAsync(Example example)
    {
        string? result = await Microsoft.Maui.Controls.Application.Current!.Windows[0].Page!.DisplayPromptAsync(
            "Edit Example", "Enter new example:", initialValue: example.Value, maxLength: 500, keyboard: Keyboard.Default);

        if (!string.IsNullOrWhiteSpace(result) && result != example.Value)
        {
            example.Value = result.Trim();
        }
    }
}