using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatAPI.Entities;

public enum ChatRoomType
{
    Private = 0, // 好友聊天
    Group = 1    // 群組聊天
}

public class ChatRoom
{
    public int Id { get; set; }
    public string? Roomname { get; set; } // 好友為空

    public byte[]? PhotoImg { get; set; } //群組照片 未貼或好友時為空
    public int CreatedByUserId { get; set; }
    public ChatRoomType RoomType { get; set; } // 0: 好友, 1: 群组
    
    [ForeignKey("Friendship")]
    public int? FriendshipId { get; set; }
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdateAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false; //聊天室是否被刪除

    // 導覽屬性
    public Friendship? Friendship { get; set; } // 外鍵和導航属性
    public User CreatedBy { get; set; } = null!;
    public ICollection<UserChatRoom> UserChatRooms { get; set; } = new List<UserChatRoom>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();

}