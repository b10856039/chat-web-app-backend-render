using System;
using ChatAPI.DTO;
using ChatAPI.DTO.ChatRoom;
using ChatAPI.Entities;

namespace ChatAPI.Mapping;

public static class ChatRoomMapping
{
    public static ChatroomDTO ToChatroomDTO(this ChatRoom room, int userId)
    {
        string displayRoomName = room.Roomname ?? "Unknown"; // 預設使用 Roomname
        string displayPhotoImg = room.PhotoImg != null ? Convert.ToBase64String(room.PhotoImg) : "";

        // 確認是私人聊天室並且 Friendship 狀態是 Accepted
        if (room.RoomType == ChatRoomType.Private && room.Friendship != null && room.Friendship.Status == FriendshipState.Accepted)
        {
            // 找到對方的名稱
            if (room.Friendship.RequesterId == userId)
            {
                displayRoomName = room.Friendship.Receiver?.Username ?? "Unknown";
                displayPhotoImg = room.Friendship.Receiver?.PhotoImg != null ? Convert.ToBase64String(room.Friendship.Receiver.PhotoImg) : "";

            }
            else if (room.Friendship.ReceiverId == userId)
            {
                displayRoomName = room.Friendship.Requester?.Username ?? "Unknown";
                displayPhotoImg = room.Friendship.Requester?.PhotoImg != null ? Convert.ToBase64String(room.Friendship.Requester.PhotoImg) : "";
            }
        }

        return new ChatroomDTO(
            room.Id,
            displayRoomName, // 替換為好友名稱
            displayPhotoImg,
            room.CreatedByUserId,
            room.CreatedBy.Username,
            (int)room.RoomType,
            room.FriendshipId,
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
