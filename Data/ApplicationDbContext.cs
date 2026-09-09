using DocCapture.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocCapture.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();
        public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
        public DbSet<Batch> Batches => Set<Batch>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<ExtractedField> ExtractedFields => Set<ExtractedField>();
        public DbSet<FieldChangeLog> FieldChangeLogs => Set<FieldChangeLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ExtractedField>()
                .HasIndex(e => new { e.DocumentId, e.FieldDefinitionId })
                .IsUnique();

            builder.Entity<ExtractedField>()
                .HasOne(e => e.FieldDefinition)
                .WithMany()
                .HasForeignKey(e => e.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Batch>()
                .HasOne(b => b.FormTemplate)
                .WithMany()
                .HasForeignKey(b => b.FormTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExtractedField>()
                .Property(e => e.Confidence)
                .HasPrecision(5, 4);
        }
    }
}