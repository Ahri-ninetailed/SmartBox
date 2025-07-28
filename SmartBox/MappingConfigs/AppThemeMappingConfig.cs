using Mapster;
using Syncfusion.Maui.Themes;

namespace SmartBox.MappingConfigs
{
    public class AppThemeMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<AppTheme, SfVisuals>().MapWith(src => MapAppTheme(src));
        }

        public static SfVisuals MapAppTheme(AppTheme src)
        {
            switch (src)
            {
                case AppTheme.Dark:
                    return SfVisuals.MaterialDark;
                case AppTheme.Light:
                    return SfVisuals.MaterialLight;
                default:
                    throw new InvalidOperationException($"Unknown enum value: {src}");
            }
        }
    }
}
