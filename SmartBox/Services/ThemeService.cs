using SmartBox.Interfaces;

namespace SmartBox.Services
{
    public class ThemeService : IThemeService
    {
        public void SetTheme(AppTheme theme)
        {
            Application.Current!.UserAppTheme = theme;
        }
    }
}
