using ChatAPI.Entities;

namespace ChatAPI.DTO.FriendShip;

public record class FriendShipDTO
(
    int FriendShipId,
    int FriendId,
    string FriendUsername,
    string FriendShowname,
    string FriendPhotoImg,
    FriendshipState Status
);