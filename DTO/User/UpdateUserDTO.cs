using System.ComponentModel.DataAnnotations;
using ChatAPI.Entities;

namespace ChatAPI.DTO;

public record class UpdatePutUserDTO
(
    string Username,
    string ShowUsername,
    string Email,
    string Phone,
    string Password,
    UserState State,
    string PhotoImg,
    UserRole Role
);


public record class UpdatePatchUserDTO
{
    [RegularExpression("^[a-zA-Z0-9_.]+$", ErrorMessage = "使用者名稱只能包含英文、數字、底線 (_) 和句號 (.)")]
    public string? Username { get; init; }

    // 顯示名稱沒有正則驗證規則，但仍然可以為 null
    public string? ShowUsername { get; init; }

    [EmailAddress(ErrorMessage = "請輸入正確的信箱格式")]
    public string? Email { get; init; }

    [RegularExpression("^[0-9]{10}$", ErrorMessage = "手機號碼須為 10 位數字")]
    public string? Phone { get; init; }

    // 密碼規範不一定要在這裡，假設密碼可以為 null（可選）
    [MinLength(3, ErrorMessage = "密碼長度至少 3 個字元")]
    public string? OldPassword { get; init; }

    [MinLength(3, ErrorMessage = "新密碼長度至少 3 個字元")]
    public string? NewPassword { get; init; }

    public UserState? State { get; init; }

    public string? PhotoImg { get; init; }

    public UserRole? Role { get; init; }
}


