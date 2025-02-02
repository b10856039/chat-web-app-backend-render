namespace ChatAPI.DTO;

public record class CreateChatroomDTO
(
    string? Roomname,
    int CreatedByUserId,
    int? ReceiverFriendshipId,
    int RoomType
);
