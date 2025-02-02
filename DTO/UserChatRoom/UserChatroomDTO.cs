using ChatAPI.Entities;

namespace ChatAPI.DTO.UserChatRoom;

public record class UserChatroomDTO
(
    int UserId,
    int ChatRoomId,
    UserRole Role,
    bool IsActive,
    bool IsBanned
);
