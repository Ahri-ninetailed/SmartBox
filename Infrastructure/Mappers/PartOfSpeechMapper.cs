using Shared;

namespace Infrastructure.Mappers
{
    public class PartOfSpeechMapper : IMapper<Models.PartOfSpeech?, Domain.Models.PartOfSpeech>,
                                      IMapper<Domain.Models.PartOfSpeech, Models.PartOfSpeech?>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Domain.Models.PartOfSpeech Map(Models.PartOfSpeech? source)
        {
            switch (source)
            {
                case Models.PartOfSpeech.Noun:
                case Models.PartOfSpeech.Other:
                    return Domain.Models.PartOfSpeech.Other;
                case Models.PartOfSpeech.Verb:
                    return Domain.Models.PartOfSpeech.Verb;
                case null:
                    return Domain.Models.PartOfSpeech.Unknown;
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }

        public Models.PartOfSpeech? Map(Domain.Models.PartOfSpeech source)
        {
            switch (source)
            {
                case Domain.Models.PartOfSpeech.Verb:
                    return Models.PartOfSpeech.Verb;
                case Domain.Models.PartOfSpeech.Other:
                    return Models.PartOfSpeech.Other;
                case Domain.Models.PartOfSpeech.Unknown:
                    return null;// TODO: сделать в бд такой же тип частей речей как и в Domain
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }
    }
}
