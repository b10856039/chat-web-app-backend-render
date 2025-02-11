using System;
using Microsoft.EntityFrameworkCore;

namespace ChatAPI.Entities;

[Index(nameof(UserId), nameof(ChatRoomId), IsUnique = true)]
public class UserChatRoom
{
    public int UserId { get; set; }
    public int ChatRoomId { get; set; }
    public UserRole Role { get; set; } = UserRole.Member; //使用者的權限
    public bool IsActive { get; set; } = true; //是否可以在聊天室
    public bool IsBanned { get; set; } = false; //是否被ban
    public DateTime? KickoutTime { get; set; }

    // 導覽屬性
    public User User { get; set; } = null!;
    public ChatRoom ChatRoom { get; set; } = null!;
}