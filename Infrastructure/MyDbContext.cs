using Infrastructure.Models.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class MyDbContext : DbContext
    {
        public required DbSet<Word> EnglishWords { get; set; }
        public required DbSet<Definition> Definitions { get; set; }
        public required DbSet<Example> Examples { get; set; }


        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Example>(entity =>
            {
                entity.ToTable("examples");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnName("id");

                entity.Property(e => e.DefinitionId)
                      .HasColumnName("definition_id")
                      .IsRequired();

                entity.Property(e => e.Value)
                      .HasColumnName("example")
                      .IsRequired();

                entity.HasOne(e => e.Definition)
                      .WithMany(d => d.Examples)
                      .HasForeignKey(e => e.DefinitionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Definition>(entity =>
            {
                entity.ToTable("definitions");

                entity.HasKey(d => d.Id);

                entity.Property(d => d.Id)
                      .HasColumnName("id");

                entity.Property(d => d.WordId)
                      .HasColumnName("word_id")
                      .IsRequired();

                entity.Property(d => d.Value)
                      .HasColumnName("definition")
                      .IsRequired();

                entity.HasOne(d => d.Word)
                      .WithMany(w => w.Definitions)
                      .HasForeignKey(d => d.WordId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Word>(entity =>
            {
                entity.ToTable("englishwords");

                entity.HasKey(e => e.Id);

                /*   entity.Property(e => e.Id)
                         .ValueGeneratedOnAdd();*/

                entity.Property(e => e.Value)
                      .HasColumnName("word")
                      .IsRequired();

                entity.Property(e => e.Popularity)
                      .HasColumnName("popularity");

                entity.Property(e => e.Id)
                      .HasColumnName("id");

                entity.Property(e => e.ShouldStudy)
                      .HasColumnName("should_study");

                entity.Property(e => e.Learned)
                      .HasColumnName("learned");

                entity.Property(e => e.Comments)
                      .HasColumnName("comments");

                entity.Property(e => e.PartOfSpeech)
                      .HasColumnName("speech_part")
                      .HasConversion(v => v.HasValue ? v.Value.ToString().ToLower() : null,                                        // преобразование enum в строку с маленькими буквами
                                     v => !string.IsNullOrEmpty(v) ? (PartOfSpeech)Enum.Parse(typeof(PartOfSpeech), v, true) : null);// преобразование строки обратно в enum

                entity.HasMany(w => w.Definitions)
                      .WithOne(d => d.Word)
                      .HasForeignKey(d => d.WordId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.Value, e.PartOfSpeech })
                      .IsUnique()
                      .HasDatabaseName("unique_speechpart_and_word");
            });
        }
    }
}
