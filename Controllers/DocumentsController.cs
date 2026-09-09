using DocCapture.Data;
using DocCapture.Models.Entities;
using DocCapture.Models.ViewModels;
using DocCapture.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocCapture.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IDocumentStorage _storage;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            ApplicationDbContext db,
            IDocumentStorage storage,
            UserManager<IdentityUser> userManager,
            ILogger<DocumentsController> logger)
        {
            _db = db;
            _storage = storage;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Upload(int batchId)
        {
            var batch = await _db.Batches
                .Include(b => b.Documents)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch is null) return NotFound();
            return View(batch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52_428_800)]
        public async Task<IActionResult> Upload(int batchId, List<IFormFile> files)
        {
            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch is null) return NotFound();

            if (files is null || files.Count == 0)
            {
                TempData["Error"] = "No files were selected.";
                return RedirectToAction(nameof(Upload), new { batchId });
            }

            var fieldDefs = await _db.FieldDefinitions
                .Where(f => f.FormTemplateId == batch.FormTemplateId)
                .OrderBy(f => f.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            if (fieldDefs.Count == 0)
            {
                TempData["Error"] = "This batch's form template has no fields defined.";
                return RedirectToAction(nameof(Upload), new { batchId });
            }

            var saved = 0;
            var errors = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var path = await _storage.SaveAsync(file, batchId);

                    var doc = new Document
                    {
                        BatchId = batchId,
                        OriginalFileName = Path.GetFileName(file.FileName),
                        StoragePath = path,
                        Status = DocumentStatus.Pending,
                        UploadedAtUtc = DateTime.UtcNow,
                        Fields = fieldDefs.Select(fd => new ExtractedField
                        {
                            FieldDefinitionId = fd.Id,
                            IsVerified = false
                        }).ToList()
                    };

                    _db.Documents.Add(doc);
                    saved++;
                }
                catch (ArgumentException ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                    _logger.LogWarning(ex, "Rejected upload {File}", file.FileName);
                }
            }

            await _db.SaveChangesAsync();

            TempData["Message"] = $"{saved} file(s) uploaded.";
            if (errors.Count > 0) TempData["Error"] = string.Join(" | ", errors);

            return RedirectToAction("Details", "Batches", new { id = batchId });
        }

        public async Task<IActionResult> Encode(int id)
        {
            var doc = await _db.Documents
                .Include(d => d.Batch)
                .Include(d => d.Fields)
                    .ThenInclude(f => f.FieldDefinition)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc is null) return NotFound();

            var siblingIds = await _db.Documents
                .Where(d => d.BatchId == doc.BatchId)
                .OrderBy(d => d.Id)
                .Select(d => d.Id)
                .ToListAsync();

            var index = siblingIds.IndexOf(doc.Id);

            var vm = new EncodeViewModel
            {
                DocumentId = doc.Id,
                BatchId = doc.BatchId,
                BatchName = doc.Batch?.Name ?? "",
                OriginalFileName = doc.OriginalFileName,
                ImageUrl = _storage.GetPublicUrl(doc.StoragePath),
                Status = doc.Status,
                PositionInBatch = index + 1,
                BatchTotal = siblingIds.Count,
                NextDocumentId = index >= 0 && index < siblingIds.Count - 1
                    ? siblingIds[index + 1]
                    : null,
                Fields = doc.Fields
                    .OrderBy(f => f.FieldDefinition!.DisplayOrder)
                    .Select(f => new EncodeFieldViewModel
                    {
                        ExtractedFieldId = f.Id,
                        FieldKey = f.FieldDefinition!.FieldKey,
                        DisplayLabel = f.FieldDefinition.DisplayLabel,
                        DataType = f.FieldDefinition.DataType,
                        IsRequired = f.FieldDefinition.IsRequired,
                        ValidationRegex = f.FieldDefinition.ValidationRegex,
                        RawValue = f.RawValue,
                        CorrectedValue = f.CorrectedValue,
                        Confidence = f.Confidence,
                        IsVerified = f.IsVerified
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveField([FromBody] SaveFieldRequest request)
        {
            var field = await _db.ExtractedFields
                .Include(f => f.FieldDefinition)
                .FirstOrDefaultAsync(f => f.Id == request.ExtractedFieldId);

            if (field is null) return NotFound(new { message = "Field not found." });

            var newValue = string.IsNullOrWhiteSpace(request.Value) ? null : request.Value.Trim();
            var oldValue = field.CorrectedValue;

            if (oldValue == newValue)
                return Json(new { saved = false, unchanged = true });

            var def = field.FieldDefinition!;
            if (!string.IsNullOrEmpty(newValue) && !string.IsNullOrEmpty(def.ValidationRegex))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        newValue, def.ValidationRegex,
                        System.Text.RegularExpressions.RegexOptions.None,
                        TimeSpan.FromMilliseconds(200)))
                {
                    return BadRequest(new { message = $"{def.DisplayLabel} format is invalid." });
                }
            }

            field.CorrectedValue = newValue;
            field.IsVerified = !string.IsNullOrEmpty(newValue);

            _db.FieldChangeLogs.Add(new FieldChangeLog
            {
                ExtractedFieldId = field.Id,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedByUserId = _userManager.GetUserId(User) ?? "",
                ChangedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Json(new { saved = true, verified = field.IsVerified });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkVerified(int id)
        {
            var doc = await _db.Documents
                .Include(d => d.Fields)
                    .ThenInclude(f => f.FieldDefinition)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc is null) return NotFound(new { message = "Document not found." });

            var missing = doc.Fields
                .Where(f => f.FieldDefinition!.IsRequired && string.IsNullOrWhiteSpace(f.CorrectedValue))
                .Select(f => f.FieldDefinition!.DisplayLabel)
                .ToList();

            if (missing.Count > 0)
                return BadRequest(new { message = "Required fields are empty: " + string.Join(", ", missing) });

            doc.Status = DocumentStatus.InReview;
            await _db.SaveChangesAsync();

            return Json(new { ok = true });
        }
    }
}