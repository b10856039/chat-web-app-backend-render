using System;

namespace ChatAPI.Entities;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public int UserId { get; set; }
    public int ChatRoomId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false; // 軟刪除訊息

    // 導覽屬性
    public User User { get; set; } = null!;
    public ChatRoom ChatRoom { get; set; } = null!;

}