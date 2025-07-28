using Syncfusion.Maui.Themes;

namespace SmartBox.Interfaces
{
    public interface IThemeService
    {
        void SetTheme(AppTheme theme);

        void Initialize(SyncfusionThemeResourceDictionary source);
    }
}
