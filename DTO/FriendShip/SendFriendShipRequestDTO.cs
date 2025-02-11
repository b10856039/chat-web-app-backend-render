namespace ChatAPI.DTO.FriendShip;

public record class SendFriendShipRequestDTO
(
    int RequesterId,
    int ReceiverId
);
