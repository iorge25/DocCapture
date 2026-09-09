using DocCapture.Models.Entities;

namespace DocCapture.Models.ViewModels
{
    public class EncodeViewModel
    {
        public int DocumentId { get; set; }
        public int BatchId { get; set; }
        public string BatchName { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public DocumentStatus Status { get; set; }
        public int? NextDocumentId { get; set; }
        public int PositionInBatch { get; set; }
        public int BatchTotal { get; set; }
        public List<EncodeFieldViewModel> Fields { get; set; } = new();
    }

    public class EncodeFieldViewModel
    {
        public int ExtractedFieldId { get; set; }
        public string FieldKey { get; set; } = "";
        public string DisplayLabel { get; set; } = "";
        public FieldDataType DataType { get; set; }
        public bool IsRequired { get; set; }
        public string? ValidationRegex { get; set; }
        public string? RawValue { get; set; }
        public string? CorrectedValue { get; set; }
        public decimal? Confidence { get; set; }
        public bool IsVerified { get; set; }

        public string Value => CorrectedValue ?? RawValue ?? "";

        public string ConfidenceClass => Confidence switch
        {
            null => "conf-none",
            >= 0.90m => "conf-high",
            >= 0.70m => "conf-mid",
            _ => "conf-low"
        };

        public string InputType => DataType switch
        {
            FieldDataType.Date => "date",
            FieldDataType.Number => "number",
            _ => "text"
        };
    }

    public class SaveFieldRequest
    {
        public int ExtractedFieldId { get; set; }
        public string? Value { get; set; }
    }
}