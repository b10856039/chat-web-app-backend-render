using ChatAPI.Entities;

namespace ChatAPI.DTO;

public record class UpdatePutUserDTO
(
    string Username,
    string Email,
    string Phone,
    string Password,
    UserState State,
    string PhotoImg,
    UserRole Role
);


public record class UpdatePatchUserDTO
(
    string? Username,
    string? Email,
    string? Phone,
    string? OldPassword,
    string? NewPassword,
    UserState? State,
    string? PhotoImg,
    UserRole? Role
);



