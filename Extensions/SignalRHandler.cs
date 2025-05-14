using System;
using ChatAPI.Data;
using ChatAPI.DTO.Message;
using ChatAPI.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatAPI.Extensions;

public static class SignalRHandler
{

    public class ChatHub(ChatAPIContext dbContext) : Hub
    {
        private readonly ChatAPIContext _dbContext = dbContext;

        // 當用戶連接時將其加入聊天室
        public async Task JoinChatRoom(int chatRoomId)
        {
            // 把聊天室ID存儲在連接上下文中
            Context.Items["ChatRoomId"] = chatRoomId;

            // 將連接加入到聊天室群組
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
        }

        //當用戶連接時加入所有聊天室
        public async Task JoinAllChatRoom(int userId)
        {
            var allUserChatroom = await _dbContext.UserChatRooms.Where( uc => uc.UserId == userId && uc.IsActive && !uc.IsBanned).ToListAsync();

            foreach(var room in allUserChatroom)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{room.ChatRoomId}");
            }
        }

        public async Task LeaveChatRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            Context.Items.Remove("ChatRoomId"); // 可選：移除記錄
        }

        public async Task SendMessage(int chatRoomId, int userId, string messageContent)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            var chatRoom = await _dbContext.ChatRooms.FindAsync(chatRoomId);
            var datetime = DateTime.UtcNow;


            var userChatroom = await _dbContext.UserChatRooms.Where( ucr => ucr.UserId == userId && ucr.ChatRoomId == chatRoomId && ucr.IsBanned == false).FirstOrDefaultAsync();

            // 檢查是否找到對應的 User 和 ChatRoom
            if (user == null || chatRoom == null)
            {
                // 向當前客戶端發送錯誤消息
                await Clients.Caller.SendAsync("ReceiveError", "User or ChatRoom not found");
                return;
            }

            if(userChatroom == null)
            {
                // 向當前客戶端發送錯誤消息
                await Clients.Caller.SendAsync("ReceiveError", "User not in Chatroom or get banned");
                return;
            }

            var message = new Message
            {
                Content = messageContent,
                SentAt = datetime,
                UserId = user.Id,
                User = user, // 設置 User
                ChatRoomId = chatRoomId,
                ChatRoom = chatRoom, // 設置 ChatRoom
                UpdateAt = datetime,
                IsDeleted = false
            };

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();


            var messageDto =  new MessageDTO
                (
                    message.Id,
                    message.Content,
                    user.Id,
                    user.Username,
                    user.PhotoImg != null ? Convert.ToBase64String(user.PhotoImg) : "",
                    chatRoomId,
                    message.SentAt,
                    message.UpdateAt,
                    message.IsDeleted
            );

            await Clients.Group($"ChatRoom_{chatRoomId}").SendAsync("ReceiveMessage", 
                messageDto);


        }

        // 當用戶斷開連接時將其移出聊天室
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // 從 Context.Items 取出 chatRoomId
            if (Context.Items.ContainsKey("ChatRoomId") && Context.Items["ChatRoomId"] is int chatRoomId)
            {
                // 若 chatRoomId 存在且是有效的 int 類型，則移出群組
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
            }

            // 執行基類的 OnDisconnectedAsync 方法
            await base.OnDisconnectedAsync(exception);
        }

        
    }
}

