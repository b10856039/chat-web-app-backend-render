using System;
using ChatAPI.DTO.Message;

namespace ChatAPI.DTO;

public record class ChatroomDTO
(
    int Id,
    string? Roomname,
    int CreatedByUserId,
    string CreatedByUsername,
    int RoomType,
    int? FriendshipForeignKey,
    List<UserDTO> Participants,
    DateTime CreateAt,
    bool IsDeleted
);
