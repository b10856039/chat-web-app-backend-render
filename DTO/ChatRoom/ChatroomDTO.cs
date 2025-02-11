using System;
using ChatAPI.DTO.Message;
using ChatAPI.DTO.UserChatRoom;

namespace ChatAPI.DTO;

public record class ChatroomDTO
(
    int Id,
    string? Roomname,
    string? PhotoImg,
    int CreatedByUserId,
    string CreatedByUsername,
    int RoomType,
    int? FriendshipForeignKey,
    List<UserChatroomDTO> Participants,
    DateTime CreateAt,
    bool IsDeleted
);
