using Mapster;

namespace Infrastructure.MappingConfigs
{
    internal class PartOfSpeechMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Models.PartOfSpeech?, Domain.Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));
        }

        public static Domain.Models.PartOfSpeech MapPartOfSpeech(Models.PartOfSpeech? src)
        {
            switch (src)
            {
                case Models.PartOfSpeech.Noun:
                case Models.PartOfSpeech.Other:
                    return Domain.Models.PartOfSpeech.Other;
                case Models.PartOfSpeech.Verb:
                    return Domain.Models.PartOfSpeech.Verb;
                case null:
                    return Domain.Models.PartOfSpeech.Unknown;
                default:
                    throw new InvalidOperationException($"Unknown enum value: {src}");
            }
        }
    }
}
