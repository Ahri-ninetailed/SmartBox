using Shared;

namespace Domain.Mappers
{
    /// <summary>
    /// Mapper between Parser and Dictionary contexts for Definition/Example models
    /// </summary>
    public class DefinitionMapper : IMapper<Models.Dictionary.Definition, Models.Parser.Definition>,
                                    IMapper<Models.Parser.Definition, Models.Dictionary.Definition>
    {
        public Models.Parser.Definition Map(Models.Dictionary.Definition source)
        {
            return new Models.Parser.Definition
            {
                Id = source.Id,
                Value = source.Value,
                Examples = source.Examples.Select(e => new Models.Parser.Example
                {
                    Id = e.Id,
                    Value = e.Value
                }).ToList()
            };
        }

        public Models.Dictionary.Definition Map(Models.Parser.Definition source)
        {
            return new Models.Dictionary.Definition
            {
                Id = source.Id,
                Value = source.Value,
                Examples = source.Examples.Select(e => new Models.Dictionary.Example
                {
                    Id = e.Id,
                    Value = e.Value,
                    DefinitionId = source.Id
                }).ToList()
            };
        }
    }
}
