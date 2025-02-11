namespace ChatAPI.DTO;

public record class CreateChatroomDTO
(
    string? Roomname,
    string? PhotoImg,
    int CreatedByUserId,
    int? ReceiverFriendshipId,
    int RoomType
);
