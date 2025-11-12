using Shared;

namespace Domain.Mappers
{
    /// <summary>
    /// Mapper between Parser and Dictionary PartOfSpeech enums
    /// </summary>
    public class PartOfSpeechMapper : IMapper<Models.Dictionary.PartOfSpeech, Models.Parser.PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Models.Parser.PartOfSpeech Map(Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Models.Dictionary.PartOfSpeech.Verb => Models.Parser.PartOfSpeech.Verb,
                Models.Dictionary.PartOfSpeech.Other => Models.Parser.PartOfSpeech.Other,
                Models.Dictionary.PartOfSpeech.Unknown => Models.Parser.PartOfSpeech.Unknown,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source))
            };
        }
    }
}
