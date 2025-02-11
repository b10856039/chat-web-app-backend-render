using System;
using ChatAPI.DTO.FriendShip;
using ChatAPI.Entities;

namespace ChatAPI.Mapping;

public static class FriendshipMapping
{
    public static FriendShipDTO ToFriendshipDTO(this Friendship friendship, int userId)
    {
        var friend = friendship.RequesterId == userId ? friendship.Receiver : friendship.Requester;
        return new FriendShipDTO(
            friendship.Id,
            friend.Id,
            friend.Username,
            friend.ShowUsername,
            friend.PhotoImg != null ? Convert.ToBase64String(friend.PhotoImg) : "",
            friendship.Status
        );
    }

    public static FriendShipNonDTO ToFriendshipNonDTO(this User user)
    {
        return new FriendShipNonDTO(
            user.Id,
            user.Username,
            user.ShowUsername,
            user.PhotoImg != null ? Convert.ToBase64String(user.PhotoImg) : ""
        );
    }

    public static Friendship ToEntity(this SendFriendShipRequestDTO requester)
    {
        return new Friendship()
        {
            RequesterId = requester.RequesterId,
            ReceiverId = requester.ReceiverId
        };
    }
}
