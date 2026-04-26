using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAnalyzer.Data;
using SmartAnalyzer.Models;
using SmartAnalyzer.Services;

namespace SmartAnalyzer.Controllers;

[Authorize]
public class FileController : Controller
{
    private readonly IExcelService _excelService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly string[] AllowedExtensions = [".xlsx", ".xls"];

    public FileController(IExcelService excelService, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _excelService = excelService;
        _context = context;
        _userManager = userManager;
    }

    // ── Preview (AJAX, no DB write) ───────────────────────────────────────────

    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Preview(IFormFile file, int sheetIndex = 0)
    {
        if (file == null || file.Length == 0)
            return Json(new { error = "لم يتم اختيار ملف." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Json(new { error = "يجب أن يكون الملف بصيغة Excel (.xlsx أو .xls)." });

        using var stream = file.OpenReadStream();
        var preview = await _excelService.PreviewExcelAsync(stream, sheetIndex);
        return Json(preview);
    }

    // ── Upload ────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(IFormFile file, int headerRow = 1, int sheetIndex = 0)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "الرجاء اختيار ملف Excel.";
            return RedirectToAction("Index", "Dashboard");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            TempData["Error"] = "يجب أن يكون الملف بصيغة Excel (.xlsx أو .xls).";
            return RedirectToAction("Index", "Dashboard");
        }

        var userId = _userManager.GetUserId(User)!;

        using var stream = file.OpenReadStream();
        var uploadedFile = await _excelService.SaveFileDataAsync(
            stream, file.FileName, userId,
            Math.Max(1, headerRow),
            Math.Max(0, sheetIndex));

        TempData["Success"] = $"تم رفع الملف بنجاح! {uploadedFile.TotalRows:N0} صف و {uploadedFile.TotalColumns} عمود.";
        return RedirectToAction("Result", "Analysis", new { fileId = uploadedFile.Id });
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var file = await _context.UploadedFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

        if (file != null)
        {
            _context.UploadedFiles.Remove(file);
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حذف الملف بنجاح.";
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
