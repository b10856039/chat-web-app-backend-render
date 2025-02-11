using System;
using ChatAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatAPI.Data;

public class ChatAPIContext(DbContextOptions<ChatAPIContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    public DbSet<UserChatRoom> UserChatRooms => Set<UserChatRoom>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Friendship> Friendships => Set<Friendship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // UserChatroom複合鍵
        modelBuilder.Entity<UserChatRoom>()
            .HasKey(uc => new { uc.UserId, uc.ChatRoomId });

        modelBuilder.Entity<UserChatRoom>()
            .HasIndex(uc => new { uc.UserId, uc.ChatRoomId });

        modelBuilder.Entity<UserChatRoom>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.UserChatRooms)
            .HasForeignKey(uc => uc.UserId);

        modelBuilder.Entity<UserChatRoom>()
            .HasOne(uc => uc.ChatRoom)
            .WithMany(c => c.UserChatRooms)
            .HasForeignKey(uc => uc.ChatRoomId);

        // Friendship 雙向關係
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Requester)
            .WithMany(u => u.FriendshipsInitiated)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Receiver)
            .WithMany(u => u.FriendshipsReceived)
            .HasForeignKey(f => f.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Friendship 唯一性約束
        modelBuilder.Entity<Friendship>()
            .HasIndex(f => new { f.RequesterId, f.ReceiverId })
            .IsUnique();

        // ChatRoom 和 User 外鍵設置
        modelBuilder.Entity<ChatRoom>()
            .HasOne(cr => cr.CreatedBy)
            .WithMany() // 不需要反向關聯
            .HasForeignKey(cr => cr.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // 防止用戶刪除時自動刪除聊天室

        // ChatRoom 和 Friendship 外鍵設置
        modelBuilder.Entity<ChatRoom>()
            .HasOne(cr => cr.Friendship)
            .WithOne(f => f.PrivateChatroom) // 確保雙向關聯
            .HasForeignKey<ChatRoom>(cr => cr.FriendshipId)  // 明確指定外鍵
            .OnDelete(DeleteBehavior.SetNull); // 防止 Friendship 被刪除時設置 FriendshipId 為 null
            
        modelBuilder.Entity<Message>()
            .HasKey(m => m.Id); // 設定主鍵

        modelBuilder.Entity<Message>()
            .HasOne(m => m.User) // 每條消息由一個 User 發送
            .WithMany(u => u.Messages) // 用戶有多條消息
            .HasForeignKey(m => m.UserId) // 使用 UserId 作為外鍵
            .OnDelete(DeleteBehavior.Restrict); // 防止用戶刪除時自動刪除消息

        modelBuilder.Entity<Message>()
            .HasOne(m => m.ChatRoom) // 每條消息屬於一個聊天室
            .WithMany(c => c.Messages) // 聊天室有多條消息
            .HasForeignKey(m => m.ChatRoomId) // 使用 ChatRoomId 作為外鍵
            .OnDelete(DeleteBehavior.SetNull); // 如果聊天室刪除，則刪除該聊天室的所有消息


    }
}
