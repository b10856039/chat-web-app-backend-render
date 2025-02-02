using System;
using ChatAPI.Data;
using ChatAPI.DTO.Message;
using ChatAPI.Entities;
using ChatAPI.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static ChatAPI.Extensions.SignalRHandler;

namespace ChatAPI.Endpoints
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatAPIContext _dbContext;

        public MessageController(IHubContext<ChatHub> hubContext, ChatAPIContext dbContext)
        {
            _hubContext = hubContext;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistoryMessages([FromQuery] int userId, [FromQuery] int chatroomId, [FromQuery] bool latestOne)
        {
            UserChatRoom? userChatroom = await _dbContext.UserChatRooms
                                .Where( ucr => ucr.UserId == userId && ucr.ChatRoomId == chatroomId)
                                .FirstOrDefaultAsync();
                                
            if(userChatroom is null || userChatroom.IsBanned is true)
            {
                return NotFound();
            }

            List<MessageDTO>? messages = [];

            if(latestOne){
                messages = await _dbContext.Messages
                                .Include( m => m.User)
                                .Where( m => m.ChatRoomId == chatroomId)
                                .OrderByDescending(m => m.SentAt) 
                                .Select( m => m.ToMessageDTO())
                                .AsNoTracking()
                                .Take(1) 
                                .ToListAsync();
            }else
            {
                messages = await _dbContext.Messages
                                .Include( m => m.User)
                                .Where( m => m.ChatRoomId == chatroomId)
                                .Select( m => m.ToMessageDTO())
                                .AsNoTracking()
                                .ToListAsync();
            }

            return Ok(messages);
        }

    }
}

