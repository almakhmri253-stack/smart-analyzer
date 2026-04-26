using System.ComponentModel.DataAnnotations;

namespace SmartAnalyzer.ViewModels;

public class UserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [MinLength(6, ErrorMessage = "كلمة المرور 6 أحرف على الأقل")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "الصلاحية مطلوبة")]
    public string Role { get; set; } = "User";
}

public class EditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "الصلاحية مطلوبة")]
    public string Role { get; set; } = "User";

    [MinLength(6, ErrorMessage = "كلمة المرور 6 أحرف على الأقل")]
    public string? NewPassword { get; set; }
}
