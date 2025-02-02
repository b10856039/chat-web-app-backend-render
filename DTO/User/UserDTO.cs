
using ChatAPI.Entities;

namespace ChatAPI.DTO;

public record class UserDTO
(
    int Id,
    string Username, 
    string Email, 
    string Phone,
    UserState State, 
    string PhotoImg,
    UserRole Role,
    DateTime CreateAt,
    DateTime UpdateAt,
    bool IsDeleted
);

