using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAnalyzer.Data;
using SmartAnalyzer.Models;
using SmartAnalyzer.Services;
using SmartAnalyzer.ViewModels;
using System.Text.Json;

namespace SmartAnalyzer.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFilterService _filterService;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IFilterService filterService)
    {
        _context = context;
        _userManager = userManager;
        _filterService = filterService;
    }

    public async Task<IActionResult> Index(int? fileId)
    {
        var userId = _userManager.GetUserId(User)!;

        var files = await _context.UploadedFiles
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            UserFiles = files,
            TotalFiles = files.Count,
            TotalRecords = await _context.DataRecords.CountAsync(d =>
                _context.UploadedFiles.Where(f => f.UserId == userId).Select(f => f.Id).Contains(d.UploadedFileId))
        };

        if (fileId.HasValue)
        {
            var file = files.FirstOrDefault(f => f.Id == fileId);
            if (file != null)
            {
                vm.SelectedFile = await _context.UploadedFiles
                    .Include(f => f.FileColumns)
                    .FirstOrDefaultAsync(f => f.Id == fileId);

                vm.Columns = vm.SelectedFile?.FileColumns
                    .OrderBy(c => c.ColumnIndex)
                    .Select(c => c.ColumnName)
                    .ToList() ?? new();

                vm.TotalRows = file.TotalRows;

                var previewJson = await _context.DataRecords
                    .Where(d => d.UploadedFileId == fileId)
                    .OrderBy(d => d.RowIndex)
                    .Take(5)
                    .Select(d => d.JsonData)
                    .ToListAsync();

                vm.PreviewData = previewJson
                    .Select(j => JsonSerializer.Deserialize<Dictionary<string, string>>(j) ?? new())
                    .ToList();

                vm.SavedFilters = await _filterService.GetSavedFiltersAsync(fileId.Value, userId);
            }
        }

        return View(vm);
    }
}
