using Mapster;

namespace Infrastructure.MappingConfigs
{
    public class PartOfSpeechMappingConfig : IRegister
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Models.PartOfSpeech?, Domain.Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));

            config.NewConfig<Domain.Models.PartOfSpeech, Models.PartOfSpeech?>().MapWith(src => MapPartOfSpeech(src));

        }

        private static Domain.Models.PartOfSpeech MapPartOfSpeech(Models.PartOfSpeech? src)
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
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, src));
            }
        }

        private static Models.PartOfSpeech? MapPartOfSpeech(Domain.Models.PartOfSpeech src)
        {
            switch (src)
            {
                case Domain.Models.PartOfSpeech.Verb:
                    return Models.PartOfSpeech.Verb;
                case Domain.Models.PartOfSpeech.Other:
                    return Models.PartOfSpeech.Other;
                case Domain.Models.PartOfSpeech.Unknown:
                    return null;// TODO: сделать в бд такой же тип частей речей как и в Domain
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, src));
            }
        }
    }
}
