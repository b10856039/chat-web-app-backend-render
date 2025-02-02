using System;

namespace ChatAPI.Entities;

public class ChatRoom
{
    public int Id { get; set; }
    public string? Roomname { get; set; } // 好友為空
    public int CreatedByUserId { get; set; }
    public int RoomType { get; set; } // 0: 好友, 1: 群组
    public int? FriendshipForeignKey { get; set; } // 好友聊天室與好友關聯
    public DateTime CreateAt { get; set; } = ConvertToTaipeiTime(DateTime.UtcNow);
    public DateTime UpdateAt { get; set; } = ConvertToTaipeiTime(DateTime.UtcNow);
    public bool IsDeleted { get; set; } = false;

    // 導覽屬性
    public Friendship? Friendship { get; set; } // 外鍵和导航属性
    public User CreatedBy { get; set; } = null!;
    public ICollection<UserChatRoom> UserChatRooms { get; set; } = new List<UserChatRoom>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();

    private static DateTime ConvertToTaipeiTime(DateTime utcDateTime)
    {
        var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        var taipeiTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, taipeiTimeZone);
        return taipeiTime.AddTicks(-(taipeiTime.Ticks % TimeSpan.TicksPerSecond));
    }
}