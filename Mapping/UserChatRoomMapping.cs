using System;
using ChatAPI.DTO.UserChatRoom;
using ChatAPI.Entities;

namespace ChatAPI.Mapping;

public static class UserChatRoomMapping
{
    public static UserChatroomDTO ToUserChatroomDTO(this UserChatRoom userChatroom)
    {
        return new(
            userChatroom.UserId,
            userChatroom.ChatRoomId,
            userChatroom.Role,
            userChatroom.IsActive,
            userChatroom.IsBanned
        );
    }
}
