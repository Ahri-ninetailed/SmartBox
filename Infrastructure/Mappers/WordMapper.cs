using Shared;

namespace Infrastructure.Mappers
{
    public class WordMapper : IMapper<Models.DataBase.Word, Domain.Models.Dictionary.Word>, 
                              IMapper<Domain.Models.Dictionary.Word, Models.DataBase.Word>,
                              IMapper<Models.Parser.Word, Domain.Models.Parser.Word>
    {
        private const string ValueSeparator = ", ";

        private readonly IMapper<Models.DataBase.PartOfSpeech?, Domain.Models.Dictionary.PartOfSpeech> _partOfSpeechToDomDictionaryMapper;
        private readonly IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.DataBase.PartOfSpeech?> _partOfSpeechToInfsDBMapper;
        private readonly IMapper<Models.Parser.PartsOfSpeech, Domain.Models.Parser.PartsOfSpeech> _partOfSpeechToDomParserMapper;

        public WordMapper(IMapper<Models.DataBase.PartOfSpeech?, Domain.Models.Dictionary.PartOfSpeech> partOfSpeechToDomDictionaryMapper,
                          IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.DataBase.PartOfSpeech?> partOfSpeechToInfsDBMapper,
                          IMapper<Models.Parser.PartsOfSpeech, Domain.Models.Parser.PartsOfSpeech> partOfSpeechToDomParserMapper)
        {
            _partOfSpeechToDomDictionaryMapper = partOfSpeechToDomDictionaryMapper;
            _partOfSpeechToInfsDBMapper = partOfSpeechToInfsDBMapper;
            _partOfSpeechToDomParserMapper = partOfSpeechToDomParserMapper; 
        }
        public Domain.Models.Dictionary.Word Map(Models.DataBase.Word source)
        {
            return new Domain.Models.Dictionary.Word()
            {
                Id = source.Id,
                Values = source.Value.Split(ValueSeparator),
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToDomDictionaryMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments ?? string.Empty
            };
        }

        public Models.DataBase.Word Map(Domain.Models.Dictionary.Word source)
        {
            return new Models.DataBase.Word()
            {
                Id = source.Id,
                Value = string.Join(ValueSeparator, source.Values),
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToInfsDBMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments
            };
        }

        public Domain.Models.Parser.Word Map(Models.Parser.Word source)
        {
            return new Domain.Models.Parser.Word()
            {
                Area = source.Area,
                Value = source.Value,
                MeaningsAndExamples = source.MeaningsAndExamples,
                OtherForms = source.OtherForms,
                PluralForms = source.PluralForms,
                Type = _partOfSpeechToDomParserMapper.Map(source.Type),
            };
        }
    }
}
