using System;

namespace ChatAPI.Entities;


public class UserChatRoom
{
    public int UserId { get; set; }
    public int ChatRoomId { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; } = false;
    public DateTime? KickoutTime { get; set; }

    // 導覽屬性
    public User User { get; set; } = null!;
    public ChatRoom ChatRoom { get; set; } = null!;
}