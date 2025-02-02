using System;

namespace ChatAPI.Entities;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public int ChatRoomId { get; set; }

    // 導覽屬性
    public User User { get; set; } = null!;
    public ChatRoom ChatRoom { get; set; } = null!;

}