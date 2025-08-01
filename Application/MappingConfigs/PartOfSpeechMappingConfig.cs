using Mapster;

namespace Application.MappingConfigs
{
    public class PartOfSpeechMappingConfig : IRegister
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Domain.Models.PartOfSpeech, Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));
            
            config.NewConfig<Models.PartOfSpeech, Domain.Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));
        }

        public static Models.PartOfSpeech MapPartOfSpeech(Domain.Models.PartOfSpeech src)
        {
            return src switch
            {
                Domain.Models.PartOfSpeech.Unknown => Models.PartOfSpeech.Unknown,
                Domain.Models.PartOfSpeech.Other => Models.PartOfSpeech.Other,
                Domain.Models.PartOfSpeech.Verb => Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, src)),
            };
        }

        public static Domain.Models.PartOfSpeech MapPartOfSpeech(Models.PartOfSpeech src)
        {
            return src switch
            {
                Models.PartOfSpeech.Unknown => Domain.Models.PartOfSpeech.Unknown,
                Models.PartOfSpeech.Other => Domain.Models.PartOfSpeech.Other,
                Models.PartOfSpeech.Verb => Domain.Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, src)),
            };
        }
    }
}
