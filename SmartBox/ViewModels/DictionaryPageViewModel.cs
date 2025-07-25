using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SmartBox.ViewModels
{
    public partial class DictionaryPageViewModel : ObservableObject
    {
        private int _count = 0;

#pragma warning disable MVVMTK0045

        [ObservableProperty]
        private string counterText = "Click me";

#pragma warning restore MVVMTK0045

        [RelayCommand]
        private void Counter()
        {
            _count++;

            CounterText = _count == 1
                ? $"Clicked {_count} time"
                : $"Clicked {_count} times";

            SemanticScreenReader.Announce(CounterText);
        }
    }
}
