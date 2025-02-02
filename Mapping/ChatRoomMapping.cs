using System;
using ChatAPI.DTO;
using ChatAPI.DTO.ChatRoom;
using ChatAPI.Entities;

namespace ChatAPI.Mapping;

public static class ChatRoomMapping
{
    public static ChatroomDTO ToChatroomDTO(this ChatRoom room)
    {
        return new ChatroomDTO(
            room.Id,
            room.Roomname,
            room.CreatedByUserId,
            room.CreatedBy.Username,
            room.RoomType,
            room.FriendshipForeignKey,
            room.UserChatRooms.Select( uc => uc.User.ToUserDTO()).ToList(),
            room.CreateAt,
            room.IsDeleted
        );
    }

    public static ChatroomSummaryDTO ToChatroomSummaryDTO(this ChatRoom room)
    {
        return new ChatroomSummaryDTO(
            room.Id,
            room.Roomname,
            room.CreatedByUserId,
            room.CreatedBy?.Username ?? "Unknown",
            room.RoomType,
            room.FriendshipForeignKey,
            room.UserChatRooms.Select(uc => uc.ToUserChatroomDTO()).ToList(),
            room.CreateAt,
            room.IsDeleted
        );
    }


    public static ChatRoom ToEntity(this CreateChatroomDTO room)
    {
        return new ChatRoom(){
            Roomname = room.Roomname,
            CreatedByUserId = room.CreatedByUserId,
            UserChatRooms = new List<UserChatRoom>(),
            Messages = new List<Message>()
        };
    }
}
