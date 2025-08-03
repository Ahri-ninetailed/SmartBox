using Domain.Models.Dictionary;
using Shared;

namespace Application.Mappers
{
    public class PartOfSpeechMapper : IMapper<PartOfSpeech, Models.PartOfSpeech>,
                                      IMapper<Models.PartOfSpeech, PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Models.PartOfSpeech Map(PartOfSpeech source)
        {
            return source switch
            {
                PartOfSpeech.Unknown => Models.PartOfSpeech.Unknown,
                PartOfSpeech.Other => Models.PartOfSpeech.Other,
                PartOfSpeech.Verb => Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public PartOfSpeech Map(Models.PartOfSpeech source)
        {
            return source switch
            {
                Models.PartOfSpeech.Unknown => PartOfSpeech.Unknown,
                Models.PartOfSpeech.Other => PartOfSpeech.Other,
                Models.PartOfSpeech.Verb => PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }
    }
}
