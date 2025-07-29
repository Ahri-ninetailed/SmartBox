using Mapster;
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

        public void SetTheme(AppTheme source)
        {
            Microsoft.Maui.Controls.Application.Current!.UserAppTheme = source;

            _syncfusionThemeDictionary!.VisualTheme = source.Adapt<SfVisuals>();
        }
    }
}
