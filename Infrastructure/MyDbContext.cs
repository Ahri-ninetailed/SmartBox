using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class MyDbContext : DbContext
    {
        public required DbSet<Word> EnglishWords { get; set; }

        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            });
        }
    }
}
