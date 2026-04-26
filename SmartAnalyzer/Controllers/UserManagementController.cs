using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartAnalyzer.Models;
using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Controllers;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.OrderBy(u => u.CreatedAt).ToList();
        var currentUserId = _userManager.GetUserId(User);

        var list = new List<UserItemViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new UserItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? "",
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = u.CreatedAt,
                IsCurrentUser = u.Id == currentUserId
            });
        }

        ViewData["Breadcrumb"] = "إدارة المستخدمين";
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            TempData["Error"] = "البريد الإلكتروني مستخدم مسبقاً.";
            return RedirectToAction(nameof(Index));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        var role = model.Role == "Admin" ? "Admin" : "User";
        await _userManager.AddToRoleAsync(user, role);

        TempData["Success"] = $"تم إنشاء المستخدم {model.FullName} بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            TempData["Error"] = "المستخدم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        await _userManager.UpdateAsync(user);

        // Update role
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var newRole = model.Role == "Admin" ? "Admin" : "User";
        await _userManager.AddToRoleAsync(user, newRole);

        // Reset password if provided
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pwResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!pwResult.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", pwResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] = $"تم تحديث بيانات {user.FullName} بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["Error"] = "لا يمكنك حذف حسابك الخاص.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "المستخدم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        var name = user.FullName;
        await _userManager.DeleteAsync(user);
        TempData["Success"] = $"تم حذف المستخدم {name}.";
        return RedirectToAction(nameof(Index));
    }
}
