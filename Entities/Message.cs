using System;

namespace ChatAPI.Entities;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; } = ConvertToTaipeiTime(DateTime.UtcNow);
    public int UserId { get; set; }
    public int ChatRoomId { get; set; }

    // 導覽屬性
    public User User { get; set; } = null!;
    public ChatRoom ChatRoom { get; set; } = null!;

    private static DateTime ConvertToTaipeiTime(DateTime utcDateTime)
    {
        // 台北時區
        var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        // 轉換為台北時間
        var taipeiTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, taipeiTimeZone);
        // 去掉毫秒部分
        return taipeiTime.AddTicks(-(taipeiTime.Ticks % TimeSpan.TicksPerSecond));
    }
}