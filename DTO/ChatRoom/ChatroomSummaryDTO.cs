using ChatAPI.DTO.UserChatRoom;
using ChatAPI.Entities;

namespace ChatAPI.DTO.ChatRoom;

public record class ChatroomSummaryDTO
(
    int Id,
    string? Roomname,
    int CreatedByUserId,
    string CreatedByUsername,
    int RoomType,
    int? FriendshipForeignKey,
    List<UserChatroomDTO> Participants,
    DateTime CreateAt,
    bool IsDeleted
);