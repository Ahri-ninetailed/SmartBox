using Shared;

namespace Infrastructure.Mappers
{
    public class PartOfSpeechMapper : IMapper<Infrastructure.Models.DataBase.PartOfSpeech?, Domain.Models.Dictionary.PartOfSpeech>,
                                      IMapper<Domain.Models.Dictionary.PartOfSpeech, Infrastructure.Models.DataBase.PartOfSpeech?>,
                                      IMapper<Infrastructure.Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Domain.Models.Dictionary.PartOfSpeech Map(Models.DataBase.PartOfSpeech? source)
        {
            switch (source)
            {
                case Models.DataBase.PartOfSpeech.Noun:
                case Models.DataBase.PartOfSpeech.Other:
                    return Domain.Models.Dictionary.PartOfSpeech.Other;
                case Models.DataBase.PartOfSpeech.Verb:
                    return Domain.Models.Dictionary.PartOfSpeech.Verb;
                case null:
                    return Domain.Models.Dictionary.PartOfSpeech.Unknown;
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }

        public Models.DataBase.PartOfSpeech? Map(Domain.Models.Dictionary.PartOfSpeech source)
        {
            switch (source)
            {
                case Domain.Models.Dictionary.PartOfSpeech.Verb:
                    return Models.DataBase.PartOfSpeech.Verb;
                case Domain.Models.Dictionary.PartOfSpeech.Other:
                    return Models.DataBase.PartOfSpeech.Other;
                case Domain.Models.Dictionary.PartOfSpeech.Unknown:
                    return null;// TODO: сделать в бд такой же тип частей речей как и в Domain
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }

        public Domain.Models.Parser.PartOfSpeech Map(Models.Parser.PartOfSpeech source)
        {
            switch (source)
            {
                case Models.Parser.PartOfSpeech.Noun:
                    return Domain.Models.Parser.PartOfSpeech.Noun;
                case Models.Parser.PartOfSpeech.Verb:
                    return Domain.Models.Parser.PartOfSpeech.Verb;
                case Models.Parser.PartOfSpeech.PhrasalVerb:
                    return Domain.Models.Parser.PartOfSpeech.PhrasalVerb;
                case Models.Parser.PartOfSpeech.Idiom:
                    return Domain.Models.Parser.PartOfSpeech.Idiom;
                case Models.Parser.PartOfSpeech.BlueRectangle:
                    return Domain.Models.Parser.PartOfSpeech.BlueRectangle;
                case Models.Parser.PartOfSpeech.Other:
                    return Domain.Models.Parser.PartOfSpeech.Other;
                case Models.Parser.PartOfSpeech.Unknown:
                    return Domain.Models.Parser.PartOfSpeech.Unknown;
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }
    }
}
