namespace ChatAPI.DTO;

public record class CreateUserDTO
(
    string Username,
    string Email,
    string Phone,
    string Password
);
