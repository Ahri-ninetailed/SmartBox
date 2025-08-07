namespace Infrastructure.Models.DataBase
{
    public class Definition
    {
        public int Id { get; set; }

        public int WordId { get; set; }

        public required string Value { get; set; }

        public virtual Word? Word { get; set; }

        public virtual ICollection<Example> Examples { get; set; } = [];
    }
}
