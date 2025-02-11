using System.ComponentModel.DataAnnotations;

namespace ChatAPI.DTO;

public record class CreateUserDTO
{
    [Required(ErrorMessage = "請輸入使用者名稱")]
    [RegularExpression("^[a-zA-Z0-9_.]+$", ErrorMessage = "使用者名稱只能包含英文、數字、底線 (_) 和句號 (.)")]
    public required string Username { get; init; }

    public required string ShowUsername { get; init; }  // 這個欄位沒有前端驗證規則，但可以自行定義

    [Required(ErrorMessage = "請輸入信箱")]
    [EmailAddress(ErrorMessage = "請輸入正確的信箱格式")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "請輸入手機")]
    [RegularExpression("^[0-9]{10}$", ErrorMessage = "手機號碼須為 10 位數字")]
    public required string Phone { get; init; }

    [Required(ErrorMessage = "請輸入密碼")]
    [MinLength(3, ErrorMessage = "密碼長度至少 3 個字元")]
    public required string Password { get; init; }
}
