using Mapster;

namespace Application.MappingConfigs
{
    public class PartOfSpeechMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Domain.Models.PartOfSpeech, Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));
        }

        public static Models.PartOfSpeech MapPartOfSpeech(Domain.Models.PartOfSpeech src)
        {
            return src switch
            {
                Domain.Models.PartOfSpeech.Unknown => Models.PartOfSpeech.Unknown,
                Domain.Models.PartOfSpeech.Other => Models.PartOfSpeech.Other,
                Domain.Models.PartOfSpeech.Verb => Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException($"Unknown enum value: {src}"),
            };
        }
    }
}
