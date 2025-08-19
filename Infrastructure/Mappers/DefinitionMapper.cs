using Shared;

namespace Infrastructure.Mappers
{
    public class DefinitionMapper : IMapper<Domain.Models.Dictionary.Definition, Infrastructure.Models.DataBase.Definition>
    {
        public Models.DataBase.Definition Map(Domain.Models.Dictionary.Definition source)
        {
            return new Models.DataBase.Definition()
            {
                Id = source.Id,
                Value = source.Value,
                Examples = source.Examples.Select(example =>
                {
                    return new Models.DataBase.Example()
                    {
                        Id = example.Id,
                        DefinitionId = example.DefinitionId,
                        Value = example.Value
                    };
                }).ToList(),
            };
        }
    }
}
