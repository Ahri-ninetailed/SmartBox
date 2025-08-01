using Application.Models;
using Shared;

namespace Application.Mapper
{
    public class WordMapper : IMapper<Domain.Models.Word, Word>,
                              IMapper<Word, Domain.Models.Word>
    {
        private readonly IMapper<Domain.Models.PartOfSpeech, PartOfSpeech> _partOfSpeechToAppMapper;
        private readonly IMapper<PartOfSpeech, Domain.Models.PartOfSpeech> _partOfSpeechToDomMapper;

        public WordMapper(IMapper<Domain.Models.PartOfSpeech, PartOfSpeech> partOfSpeechToAppMapper,
                          IMapper<PartOfSpeech, Domain.Models.PartOfSpeech> partOfSpeechToDomMapper)
        {
            _partOfSpeechToDomMapper = partOfSpeechToDomMapper;
            _partOfSpeechToAppMapper = partOfSpeechToAppMapper;
        }

        public Word Map(Domain.Models.Word source)
        {
            return new Word()
            {
                Id = source.Id,
                Value = source.Value,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToAppMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments,
            };
        }

        public Domain.Models.Word Map(Word source)
        {
            return new Domain.Models.Word()
            {
                Id = source.Id,
                Value = source.Value,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToDomMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments,
            };
        }
    }
}
