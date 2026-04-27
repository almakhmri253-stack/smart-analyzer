using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartAnalyzer.Models;
using SmartAnalyzer.Services;
using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Controllers;

[Authorize]
public class AnalysisController : Controller
{
    private readonly IFilterService _filterService;
    private readonly IExcelService _excelService;
    private readonly IAnalysisService _analysisService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BotService _botService;

    public AnalysisController(
        IFilterService filterService,
        IExcelService excelService,
        IAnalysisService analysisService,
        UserManager<ApplicationUser> userManager,
        BotService botService)
    {
        _filterService = filterService;
        _excelService = excelService;
        _analysisService = analysisService;
        _userManager = userManager;
        _botService = botService;
    }

    [HttpGet]
    public async Task<IActionResult> Result(int fileId)
    {
        var userId = _userManager.GetUserId(User)!;
        var vm = await _analysisService.GetFileAnalysisAsync(fileId, userId);
        if (vm.FileId == 0) return RedirectToAction("Index", "Dashboard");
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Compare([FromBody] ComparisonRequestViewModel request)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _analysisService.GetComparisonAsync(request, userId);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Filter([FromBody] FilterRequestViewModel request)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _filterService.ApplyFiltersAsync(request, userId);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveFilter([FromBody] FilterRequestViewModel request)
    {
        var userId = _userManager.GetUserId(User)!;
        var id = await _filterService.SaveFilterAsync(request, userId);
        return Json(new { success = true, id });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFilter(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _filterService.DeleteSavedFilterAsync(id, userId);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> SavedFilters(int fileId)
    {
        var userId = _userManager.GetUserId(User)!;
        var filters = await _filterService.GetSavedFiltersAsync(fileId, userId);
        return Json(filters);
    }

    [HttpPost]
    public async Task<IActionResult> EntityCards([FromBody] EntityCardsRequestViewModel request)
    {
        var userId = _userManager.GetUserId(User)!;
        var cards = await _filterService.GetEntityCardsAsync(request, userId);
        return Json(cards);
    }

    [HttpPost]
    public async Task<IActionResult> CrossTab([FromBody] CrossTabRequestViewModel request)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _filterService.GetCrossTabAsync(request, userId);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> BotCommand([FromBody] BotCommandRequest req)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _botService.ProcessAsync(req, userId);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> ExportExcel([FromBody] FilterRequestViewModel request)
    {
        var userId = _userManager.GetUserId(User)!;
        request.Page = 1;
        request.PageSize = int.MaxValue;
        var result = await _filterService.ApplyFiltersAsync(request, userId);
        var bytes = await _excelService.ExportToExcelAsync(result.Data, result.Columns);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "results.xlsx");
    }
}
