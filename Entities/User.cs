using System;

namespace ChatAPI.Entities;

public enum UserRole
{
    Admin = 0,
    Member = 1,
    Guest = 2
}

public enum UserState
{
    Offline = 0,
    Online = 1,
    Busy = 2,
    Hide = 3
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
    public UserState State { get; set; } = UserState.Offline;
    public byte[]? PhotoImg { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // 确保正确初始化集合
    public ICollection<UserChatRoom> UserChatRooms { get; set; } = new List<UserChatRoom>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Friendship> FriendshipsInitiated { get; set; } = new List<Friendship>();
    public ICollection<Friendship> FriendshipsReceived { get; set; } = new List<Friendship>();

}


