using Shared;

namespace Application.Mapper
{
    public class WordMapper : IMapper<Domain.Models.Dictionary.Word, Application.Models.Dictionary.Word>,
                              IMapper<Application.Models.Dictionary.Word, Domain.Models.Dictionary.Word>,
                              IMapper<Domain.Models.Parser.Word, Models.Parser.Word>,
                              IMapper<Application.Models.Parser.Word, Domain.Models.Parser.Word>,
                              IMapper<Domain.Models.Dictionary.Word, Application.Models.Parser.Word>
    {
        private readonly IMapper<Domain.Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech> _partOfSpeechToAppDictionaryMapper;
        private readonly IMapper<Application.Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> _partOfSpeechToDomDictionaryMapper;
        private readonly IMapper<Domain.Models.Parser.PartOfSpeech, Application.Models.Parser.PartOfSpeech> _partOfSpeechToAppParserMapper;
        private readonly IMapper<Application.Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech> _partOfSpeechToDomainParserMapper;
        private readonly IMapper<Domain.Models.Dictionary.PartOfSpeech, Application.Models.Parser.PartOfSpeech> _partOfSpeechDictionaryDomainToAppParserMapper;


        public WordMapper(IMapper<Domain.Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech> partOfSpeechToAppDictionaryMapper,
                          IMapper<Application.Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> partOfSpeechToDomDictionaryMapper,
                          IMapper<Domain.Models.Parser.PartOfSpeech, Application.Models.Parser.PartOfSpeech> partOfSpeechToAppParserMapper,
                          IMapper<Application.Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech> partOfSpeechToDomainParserMapper,
                          IMapper<Domain.Models.Dictionary.PartOfSpeech, Application.Models.Parser.PartOfSpeech> partOfSpeechDictionaryDomainToAppParserMapper)
        {
            _partOfSpeechToDomDictionaryMapper = partOfSpeechToDomDictionaryMapper;
            _partOfSpeechToAppDictionaryMapper = partOfSpeechToAppDictionaryMapper;
            _partOfSpeechToAppParserMapper = partOfSpeechToAppParserMapper;
            _partOfSpeechToDomainParserMapper = partOfSpeechToDomainParserMapper;
            _partOfSpeechDictionaryDomainToAppParserMapper = partOfSpeechDictionaryDomainToAppParserMapper;
        }

        public Application.Models.Dictionary.Word Map(Domain.Models.Dictionary.Word source)
        {
            return new Application.Models.Dictionary.Word()
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

        public Domain.Models.Dictionary.Word Map(Application.Models.Dictionary.Word source)
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

        public Application.Models.Parser.Word Map(Domain.Models.Parser.Word source)
        {
            return new Models.Parser.Word()
            {
                Value = source.Value,
                Area = source.Area,
                OtherForms = source.OtherForms,
                PluralForms = source.PluralForms,
                Definitions = source.MeaningsAndExamples.Select(el => 
                {
                    return new Models.Parser.Definition()
                    {
                        Id = 0,
                        Value = el.Key,
                        Examples = el.Value.Select(example => new Models.Parser.Example() 
                        { 
                            Id = 0,
                            Value = example,
                        }).ToList()
                    };  
                }).ToList(),
                Type = _partOfSpeechToAppParserMapper.Map(source.Type),
            };
        }

        public Domain.Models.Parser.Word Map(Application.Models.Parser.Word source)
        {
            return new Domain.Models.Parser.Word()
            {
                Value = source.Value,
                Area = source.Area,
                OtherForms = source.OtherForms,
                PluralForms = source.PluralForms,
                MeaningsAndExamples = source.Definitions.ToDictionary(
                    def => def.Value,
                    def => def.Examples.Select(ex => ex.Value).ToList()
                ),
                Type = _partOfSpeechToDomainParserMapper.Map(source.Type),
            };
        }

        Application.Models.Parser.Word IMapper<Domain.Models.Dictionary.Word, Application.Models.Parser.Word>.Map(Domain.Models.Dictionary.Word source)
        {
            return new Models.Parser.Word()
            {
                Value = string.Join(", ", source.Values),
                Area = null,
                PluralForms = [],
                OtherForms = [],
                Type = _partOfSpeechDictionaryDomainToAppParserMapper.Map(source.PartOfSpeech),
                Definitions = source.Definitions.Select(definition =>
                {
                    return new Models.Parser.Definition()
                    {
                        Id = definition.Id,
                        Value = definition.Value,
                        Examples = definition.Examples.Select(example => new Models.Parser.Example()
                        {
                            Id = example.Id,
                            Value = example.Value,
                        }).ToList(),
                    };
                }).ToList(),
            };
        }
    }
}
