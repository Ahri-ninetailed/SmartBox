namespace Infrastructure.Models.DataBase
{
    public class Example
    {
        public int Id { get; set; }

        public int DefinitionId { get; set; }

        public required string Value { get; set; }

        public virtual Definition? Definition { get; set; }
    }
}
