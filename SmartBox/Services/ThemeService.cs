using SmartBox.Interfaces;
using Syncfusion.Maui.Themes;

namespace SmartBox.Services
{
    public class ThemeService : IThemeService
    {
        private SyncfusionThemeResourceDictionary? _syncfusionThemeDictionary;

        public void Initialize(SyncfusionThemeResourceDictionary source)
        {
            _syncfusionThemeDictionary = source;
        }

        public void SetTheme(AppTheme theme)
        {
            Application.Current!.UserAppTheme = theme;

            _syncfusionThemeDictionary!.VisualTheme = MapTheme(theme);
        }

        // TODO: Использовать Mapster
        private static SfVisuals MapTheme(AppTheme source)
        {
            return source switch
            {
                AppTheme.Dark => SfVisuals.MaterialDark,
                AppTheme.Light => SfVisuals.MaterialLight,
                _ => throw new ArgumentException(),
            };
        }
    }
}
