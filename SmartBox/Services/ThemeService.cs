using Shared;
using SmartBox.Interfaces;
using Syncfusion.Maui.Themes;

namespace SmartBox.Services
{
    public class ThemeService : IThemeService
    {
        private SyncfusionThemeResourceDictionary? _syncfusionThemeDictionary;

        private readonly IMapper<AppTheme, SfVisuals> _appThemeMapper;

        public ThemeService(IMapper<AppTheme, SfVisuals> appThemeMapper)
        {
            _appThemeMapper = appThemeMapper;
        }

        public void Initialize(SyncfusionThemeResourceDictionary source)
        {
            _syncfusionThemeDictionary = source;
        }

        public void SetTheme(AppTheme source)
        {
            Microsoft.Maui.Controls.Application.Current!.UserAppTheme = source;

            _syncfusionThemeDictionary!.VisualTheme = _appThemeMapper.Map(source);
        }
    }
}
