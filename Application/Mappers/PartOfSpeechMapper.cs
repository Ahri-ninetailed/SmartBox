using Shared;

namespace Application.Mappers
{
    public class PartOfSpeechMapper : IMapper<Domain.Models.PartOfSpeech, Models.PartOfSpeech>,
                                      IMapper<Models.PartOfSpeech, Domain.Models.PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Models.PartOfSpeech Map(Domain.Models.PartOfSpeech source)
        {
            return source switch
            {
                Domain.Models.PartOfSpeech.Unknown => Models.PartOfSpeech.Unknown,
                Domain.Models.PartOfSpeech.Other => Models.PartOfSpeech.Other,
                Domain.Models.PartOfSpeech.Verb => Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Domain.Models.PartOfSpeech Map(Models.PartOfSpeech source)
        {
            return source switch
            {
                Models.PartOfSpeech.Unknown => Domain.Models.PartOfSpeech.Unknown,
                Models.PartOfSpeech.Other => Domain.Models.PartOfSpeech.Other,
                Models.PartOfSpeech.Verb => Domain.Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }
    }
}
