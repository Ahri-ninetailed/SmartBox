using Mapster;

namespace SmartBox.MappingConfigs
{
    public class PartOfSpeechMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Application.Models.PartOfSpeech, Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));
        }

        public static Models.PartOfSpeech MapPartOfSpeech(Application.Models.PartOfSpeech src)
        {
            return src switch
            {
                Application.Models.PartOfSpeech.Unknown => Models.PartOfSpeech.Unknown,
                Application.Models.PartOfSpeech.Other => Models.PartOfSpeech.Other,
                Application.Models.PartOfSpeech.Verb => Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException($"Unknown enum value: {src}"),
            };
        }
    }
}
