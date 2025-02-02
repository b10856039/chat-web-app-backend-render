using System;

namespace ChatAPI.Entities;

public enum FriendshipState
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Blocked = 3
}

public class Friendship
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int ReceiverId { get; set; }
    public FriendshipState Status { get; set; } = FriendshipState.Pending;
    public DateTime CreatedAt { get; set; } = ConvertToTaipeiTime(DateTime.UtcNow);
    public DateTime UpdatedAt { get; set; } = ConvertToTaipeiTime(DateTime.UtcNow);

    private static DateTime ConvertToTaipeiTime(DateTime utcDateTime)
    {
        // 台北時區
        var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        // 轉換為台北時間
        var taipeiTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, taipeiTimeZone);
        // 去掉毫秒部分
        return taipeiTime.AddTicks(-(taipeiTime.Ticks % TimeSpan.TicksPerSecond));
    }

    // 導覽屬性
    public User Requester { get; set; } = null!;
    public User Receiver { get; set; } = null!;
    public ChatRoom? PrivateChatroom { get; set; }
}
