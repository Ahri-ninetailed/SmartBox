using SmartBox.Interfaces;
using SmartBox.Views;
using Syncfusion.Maui.Themes;
namespace SmartBox
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App(AppShell appShell, IThemeService themeService)
        {
            InitializeComponent();

            MainPage = appShell;

            SyncfusionThemeResourceDictionary syncfusionThemeDictionary = Microsoft.Maui.Controls.Application.Current!.Resources.MergedDictionaries.OfType<SyncfusionThemeResourceDictionary>().First();

            themeService.Initialize(syncfusionThemeDictionary);
        }
    }
}
