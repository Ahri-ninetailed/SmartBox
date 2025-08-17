using SmartBox.Interfaces;
using SmartBox.Views;
using Syncfusion.Maui.Themes;

namespace SmartBox
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly AppShell _appShell;
        private readonly IThemeService _themeService;

        public App(AppShell appShell, IThemeService themeService)
        {
            InitializeComponent();

            _appShell = appShell;
            _themeService = themeService;

            SyncfusionThemeResourceDictionary syncfusionThemeDictionary = Microsoft.Maui.Controls.Application.Current!.Resources.MergedDictionaries.OfType<SyncfusionThemeResourceDictionary>().First();

            _themeService.Initialize(syncfusionThemeDictionary);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_appShell);
        }
    }
}