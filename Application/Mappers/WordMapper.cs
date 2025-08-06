using Application.Models;
using Shared;

namespace Application.Mapper
{
    public class WordMapper : IMapper<Domain.Models.Dictionary.Word, Models.Dictionary.Word>,
                              IMapper<Models.Dictionary.Word, Domain.Models.Dictionary.Word>,
                              IMapper<Domain.Models.Parser.Word, Models.Parser.Word>,
                              IMapper<Models.Parser.Word, Domain.Models.Parser.Word>
    {
        private readonly IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> _partOfSpeechToAppDictionaryMapper;
        private readonly IMapper<Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> _partOfSpeechToDomDictionaryMapper;
        private readonly IMapper<Domain.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> _partOfSpeechToAppParserMapper;
        private readonly IMapper<Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech> _partOfSpeechToDomainParserMapper;


        public WordMapper(IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> partOfSpeechToAppDictionaryMapper,
                          IMapper<Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> partOfSpeechToDomDictionaryMapper,
                          IMapper<Domain.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> partOfSpeechToAppParserMapper,
                          IMapper<Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech> partOfSpeechToDomainParserMapper)
        {
            _partOfSpeechToDomDictionaryMapper = partOfSpeechToDomDictionaryMapper;
            _partOfSpeechToAppDictionaryMapper = partOfSpeechToAppDictionaryMapper;
            _partOfSpeechToAppParserMapper = partOfSpeechToAppParserMapper;
            _partOfSpeechToDomainParserMapper = partOfSpeechToDomainParserMapper;
        }

        public Models.Dictionary.Word Map(Domain.Models.Dictionary.Word source)
        {
            return new Models.Dictionary.Word()
            {
                Id = source.Id,
                Values = source.Values,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToAppDictionaryMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments,
            };
        }

        public Domain.Models.Dictionary.Word Map(Models.Dictionary.Word source)
        {
            return new Domain.Models.Dictionary.Word()
            {
                Id = source.Id,
                Values = source.Values,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToDomDictionaryMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments,
            };
        }

        public Models.Parser.Word Map(Domain.Models.Parser.Word source)
        {
            return new Models.Parser.Word()
            {
                Value = source.Value,
                Area = source.Area,
                OtherForms = source.OtherForms,
                PluralForms = source.PluralForms,
                MeaningsAndExamples = source.MeaningsAndExamples,
                Type = _partOfSpeechToAppParserMapper.Map(source.Type),
            };
        }

        public Domain.Models.Parser.Word Map(Models.Parser.Word source)
        {
            return new Domain.Models.Parser.Word()
            {
                Value = source.Value,
                Area = source.Area,
                OtherForms = source.OtherForms,
                PluralForms = source.PluralForms,
                MeaningsAndExamples = source.MeaningsAndExamples,
                Type = _partOfSpeechToDomainParserMapper.Map(source.Type),
            };
        }
    }
}
