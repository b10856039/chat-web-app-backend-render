using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatAPI.Entities;

public enum FriendshipState
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}

public class Friendship
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int ReceiverId { get; set; }
    public FriendshipState Status { get; set; } = FriendshipState.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


    // 導覽屬性
    public User Requester { get; set; } = null!;
    public User Receiver { get; set; } = null!;

    public int? PrivateChatRoomId { get; set; }
    public ChatRoom? PrivateChatroom { get; set; }
}
