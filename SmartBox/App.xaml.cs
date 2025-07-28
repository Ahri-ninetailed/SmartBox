using SmartBox.Interfaces;
using SmartBox.Views;
using Syncfusion.Maui.Themes;

namespace SmartBox
{
    public partial class App : Application
    {
        public App(AppShell appShell, IThemeService themeService)
        {
            InitializeComponent();

            MainPage = appShell;

            SyncfusionThemeResourceDictionary syncfusionThemeDictionary = Application.Current!.Resources.MergedDictionaries.OfType<SyncfusionThemeResourceDictionary>().First();

            themeService.Initialize(syncfusionThemeDictionary);
        }
    }
}
