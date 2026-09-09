namespace DocCapture.Models.Entities
{
    public class FormTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();
    }

    public class FieldDefinition
    {
        public int Id { get; set; }
        public int FormTemplateId { get; set; }
        public FormTemplate? FormTemplate { get; set; }
        public string FieldKey { get; set; } = "";      // machine name, e.g. "invoice_no"
        public string DisplayLabel { get; set; } = "";
        public FieldDataType DataType { get; set; }
        public bool IsRequired { get; set; }
        public string? ValidationRegex { get; set; }
        public int DisplayOrder { get; set; }
    }

    public enum FieldDataType { Text, Number, Date, Currency }

    public class Batch
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int FormTemplateId { get; set; }
        public FormTemplate? FormTemplate { get; set; }
        public string CreatedByUserId { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class Document
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public Batch? Batch { get; set; }
        public string OriginalFileName { get; set; } = "";
        public string StoragePath { get; set; } = "";
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
        public string? RawExtractionJson { get; set; }   // unused until Phase 3
        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
        public ICollection<ExtractedField> Fields { get; set; } = new List<ExtractedField>();
    }

    public enum DocumentStatus { Pending, Extracted, InReview, Verified, Rejected }

    public class ExtractedField
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public Document? Document { get; set; }
        public int FieldDefinitionId { get; set; }
        public FieldDefinition? FieldDefinition { get; set; }
        public string? RawValue { get; set; }
        public string? CorrectedValue { get; set; }
        public decimal? Confidence { get; set; }
        public string? BoundingBoxJson { get; set; }     // unused until Phase 3
        public bool IsVerified { get; set; }
    }

    public class FieldChangeLog
    {
        public int Id { get; set; }
        public int ExtractedFieldId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string ChangedByUserId { get; set; } = "";
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
