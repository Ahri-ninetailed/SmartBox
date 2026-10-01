using SmartBox.Interfaces;
using SmartBox.ViewModels;
using SmartBox.Views;
using Syncfusion.Maui.Themes;

namespace SmartBox
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly AppShell _appShell;
        private readonly IThemeService _themeService;

        public App(AppShell appShell, IThemeService themeService, DatabaseBackupViewModel databaseBackup)
        {
            InitializeComponent();

            _appShell = appShell;
            _themeService = themeService;

            SyncfusionThemeResourceDictionary syncfusionThemeDictionary = Microsoft.Maui.Controls.Application.Current!.Resources.MergedDictionaries.OfType<SyncfusionThemeResourceDictionary>().First();

            _themeService.Initialize(syncfusionThemeDictionary);

            // Бэкап нужен один раз на запуск приложения. App — singleton, поэтому конструктор выполняется ровно один раз,
            // а OnStart MAUI вызывает на каждое созданное окно. Не ждем, ход бэкапа виден в шапке приложения
            _ = databaseBackup.RunIfDueAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_appShell);
        }
    }
}
