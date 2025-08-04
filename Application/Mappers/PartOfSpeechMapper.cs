using Domain.Models.Dictionary;
using Shared;

namespace Application.Mappers
{
    public class PartOfSpeechMapper : IMapper<PartOfSpeech, Models.Dictionary.PartOfSpeech>,
                                      IMapper<Models.Dictionary.PartOfSpeech, PartOfSpeech>,
                                      IMapper<Domain.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Models.Dictionary.PartOfSpeech Map(PartOfSpeech source)
        {
            return source switch
            {
                PartOfSpeech.Unknown => Models.Dictionary.PartOfSpeech.Unknown,
                PartOfSpeech.Other => Models.Dictionary.PartOfSpeech.Other,
                PartOfSpeech.Verb => Models.Dictionary.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public PartOfSpeech Map(Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Models.Dictionary.PartOfSpeech.Unknown => PartOfSpeech.Unknown,
                Models.Dictionary.PartOfSpeech.Other => PartOfSpeech.Other,
                Models.Dictionary.PartOfSpeech.Verb => PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Models.Parser.PartOfSpeech Map(Domain.Models.Parser.PartOfSpeech source)
        {
            return source switch
            {
                Domain.Models.Parser.PartOfSpeech.Other => Models.Parser.PartOfSpeech.Other,
                Domain.Models.Parser.PartOfSpeech.BlueRectangle => Models.Parser.PartOfSpeech.BlueRectangle,
                Domain.Models.Parser.PartOfSpeech.Idiom => Models.Parser.PartOfSpeech.Idiom,
                Domain.Models.Parser.PartOfSpeech.Noun => Models.Parser.PartOfSpeech.Noun,
                Domain.Models.Parser.PartOfSpeech.PhrasalVerb => Models.Parser.PartOfSpeech.PhrasalVerb,
                Domain.Models.Parser.PartOfSpeech.Unknown => Models.Parser.PartOfSpeech.Unknown,
                Domain.Models.Parser.PartOfSpeech.Verb => Models.Parser.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }
    }
}
