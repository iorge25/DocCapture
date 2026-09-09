using DocCapture.Data;
using DocCapture.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DocCapture.Controllers
{
    [Authorize]
    public class BatchesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public BatchesController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var batches = await _db.Batches
                .Include(b => b.FormTemplate)
                .Include(b => b.Documents)
                .OrderByDescending(b => b.CreatedAtUtc)
                .AsNoTracking()
                .ToListAsync();

            return View(batches);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateTemplatesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Batch batch)
        {
            if (!ModelState.IsValid)
            {
                await PopulateTemplatesAsync(batch.FormTemplateId);
                return View(batch);
            }

            batch.CreatedByUserId = _userManager.GetUserId(User) ?? "";
            batch.CreatedAtUtc = DateTime.UtcNow;

            _db.Batches.Add(batch);
            await _db.SaveChangesAsync();

            return RedirectToAction("Upload", "Documents", new { batchId = batch.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var batch = await _db.Batches
                .Include(b => b.FormTemplate)
                .Include(b => b.Documents)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch is null) return NotFound();
            return View(batch);
        }

        private async Task PopulateTemplatesAsync(int? selected = null)
        {
            var templates = await _db.FormTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Templates = new SelectList(templates, "Id", "Name", selected);
        }
    }
}