using Shared;

namespace Domain.Mappers
{
    /// <summary>
    /// Mapper between Parser and Dictionary contexts for Word model
    /// </summary>
    public class WordMapper : IMapper<Models.Dictionary.Word, Models.Parser.Word>
    {
        private readonly IMapper<Models.Dictionary.Definition, Models.Parser.Definition> _definitionMapper;
        private readonly IMapper<Models.Dictionary.PartOfSpeech, Models.Parser.PartOfSpeech> _partOfSpeechMapper;

        public WordMapper(
            IMapper<Models.Dictionary.Definition, Models.Parser.Definition> definitionMapper,
            IMapper<Models.Dictionary.PartOfSpeech, Models.Parser.PartOfSpeech> partOfSpeechMapper)
        {
            _definitionMapper = definitionMapper;
            _partOfSpeechMapper = partOfSpeechMapper;
        }

        public Models.Parser.Word Map(Models.Dictionary.Word source)
        {
            return new Models.Parser.Word
            {
                Id = source.Id,
                Value = string.Join(", ", source.Values),
                Area = null,
                Type = _partOfSpeechMapper.Map(source.PartOfSpeech),
                Definitions = _definitionMapper.MapToList(source.Definitions),
                PluralForms = new HashSet<string>(),
                OtherForms = new HashSet<string>()
            };
        }
    }
}
