using Shared;
using Syncfusion.Maui.Themes;

namespace SmartBox.Mappers
{
    public class AppThemeMapper : IMapper<AppTheme, SfVisuals>
    {
        public SfVisuals Map(AppTheme source)
        {
            switch (source)
            {
                case AppTheme.Dark:
                    return SfVisuals.MaterialDark;
                case AppTheme.Light:
                    return SfVisuals.MaterialLight;
                default:
                    throw new InvalidOperationException($"Unknown enum value: {source}");
            }
        }
    }
}
