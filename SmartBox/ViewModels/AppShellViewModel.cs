using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBox.Interfaces;

namespace SmartBox.ViewModels
{
    public partial class AppShellViewModel : ObservableObject
    {
        private readonly IThemeService _themeService;

        public AppShellViewModel(IThemeService themeService)
        {
            _themeService = themeService;
        }

        [RelayCommand]
        private void LightThemeClicked()
        {
            _themeService.SetTheme(AppTheme.Light);
        }

        [RelayCommand]
        private void DarkThemeClicked()
        {
            _themeService.SetTheme(AppTheme.Dark);
        }

        [RelayCommand]
        private async Task GoToDictionary()
        {
            await Shell.Current.GoToAsync("//dictionary");
        }

        [RelayCommand]
        private async Task GoToParser()
        {
            await Shell.Current.GoToAsync("//parser");
        }

        [RelayCommand]
        private async Task GoToDefinitions()
        {
            await Shell.Current.GoToAsync("//definitions");
        }
    }
}
