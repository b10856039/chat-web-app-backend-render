using ChatAPI.Entities;

namespace ChatAPI.DTO.FriendShip;

public record class FriendShipNonDTO
(
    int UserId,
    string Username,
    string ShowUsername,
    string PhotoImg
);