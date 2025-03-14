using System;
using ChatAPI.Data;
using ChatAPI.DTO.FriendShip;
using ChatAPI.Entities;
using ChatAPI.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ChatAPI.Extensions.ExceptionMiddleware;

namespace ChatAPI.Controllers
{
    [Route("api/[controller]")]
    public class FriendshipsController : ControllerBase
    {

        private readonly ChatAPIContext _dbContext;

        public FriendshipsController(ChatAPIContext dbContext)
        {
            _dbContext = dbContext;
        }


        [HttpGet("{userId}")]
        public async Task<IActionResult> GetFriendList(int userId)
        {
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted);
            if (!userExists)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "用戶不存在" }) );
            }

            var friendships = await _dbContext.Friendships
                .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) &&
                            !(f.Status == FriendshipState.Pending && f.RequesterId == userId)
                            && f.Status != FriendshipState.Rejected
                            ) // 排除自己發送但未接受的邀請
                .Include(f => f.Requester)  // 加載發送請求的用戶資訊
                .Include(f => f.Receiver)   // 加載接收請求的用戶資訊
                .Select(f => f.ToFriendshipDTO(userId))
                .ToListAsync();

            return Ok( new ApiResponse<List<FriendShipDTO>>(friendships) );
        }

        [HttpGet("non-friends/{userId}")]
        public async Task<IActionResult> GetNonFriendList(int userId, [FromQuery] string? search = null)
        {
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted);
            if (!userExists)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "用戶不存在" }) );
            }

            var friends = await _dbContext.Friendships
                .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == FriendshipState.Accepted)
                .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId)
                .ToListAsync();

            var query = _dbContext.Users
                .Where(u => u.Id != userId && !friends.Contains(u.Id) && !u.IsDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search));
            }

            var nonFriends = await query
                .Select(u => u.ToFriendshipNonDTO())
                .ToListAsync();

            return Ok( new ApiResponse<List<FriendShipNonDTO>>(nonFriends));
        }

        [Authorize]
        [HttpPost("request")]
        public async Task<IActionResult> SendFriendShipRequest([FromBody] SendFriendShipRequestDTO friendShipRequester)
        {

            if(friendShipRequester.RequesterId == friendShipRequester.ReceiverId)
            {
                return BadRequest( new ApiResponse<string>(new List<string> { "不能添加自己為好友" }) );
            }

            var requester = await _dbContext.Users.FindAsync(friendShipRequester.RequesterId);
            var receiver = await _dbContext.Users.FindAsync(friendShipRequester.ReceiverId);

            if (requester == null || receiver == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "用戶不存在" }) );
            }

            var existingFriendShip = await _dbContext.Friendships
                                        .FirstOrDefaultAsync( f =>
                                            (f.RequesterId == friendShipRequester.RequesterId && f.ReceiverId == friendShipRequester.ReceiverId) ||
                                            (f.RequesterId == friendShipRequester.ReceiverId && f.ReceiverId == friendShipRequester.RequesterId)
                                        );

            if (existingFriendShip is not null && existingFriendShip.Status == FriendshipState.Pending)
            {
                return BadRequest( new ApiResponse<string>(new List<string> { "好友請求已發送，請等待回應" }) );
            }

            try
            {
                if (existingFriendShip is not null && existingFriendShip.Status == FriendshipState.Rejected)
                {
                    // 設定 24 小時內不能再次發送
                    var lastRejectedTime = existingFriendShip.UpdatedAt;
                    var stillRejectTime = (DateTime.UtcNow - lastRejectedTime).TotalHours;
                    int hoursPerDay = 24;
                    if (stillRejectTime < hoursPerDay)
                    {
                        return BadRequest( new ApiResponse<string>(new List<string> { $"你最近才被拒絕，請等待{hoursPerDay - stillRejectTime}小時後再嘗試" }));
                    }

                    // 允許重新發送，狀態改回 Pending
                    existingFriendShip.Status = FriendshipState.Pending;
                    existingFriendShip.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    return Ok( new ApiResponse<string>("好友請求已發送") );
                }

                if(existingFriendShip is not null)
                {
                    return  BadRequest( new ApiResponse<string>(new List<string> { "好友請求已存在或已是好友" }) );
                }

                var friendship = friendShipRequester.ToEntity();

                _dbContext.Friendships.Add(friendship);

                await _dbContext.SaveChangesAsync();

                return Ok( new ApiResponse<string>("好友請求已發送") );
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        [Authorize]
        [HttpPost("respond")]
        public async Task<IActionResult> RespondToFriendRequest([FromBody] RespoendTpFriendRequestDTO toFriendRequester)
        {
            var friendship = await _dbContext.Friendships.FindAsync(toFriendRequester.FriendShipId);
            if(friendship is null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "好友請求不存在" }) );
            }

            if(friendship.Status != FriendshipState.Pending)
            {
                return BadRequest( new ApiResponse<string>(new List<string> { "好友請求已被處理" }) );
            }

            try
            {
                switch (toFriendRequester.Action.ToLower())
                {
                    case "accept":
                        friendship.Status = FriendshipState.Accepted;
                        friendship.UpdatedAt = DateTime.UtcNow;

                        var chatroom = new ChatRoom{
                            CreatedByUserId = friendship.RequesterId,
                            RoomType = ChatRoomType.Private,
                            FriendshipId = friendship.Id
                        };

                        _dbContext.ChatRooms.Add(chatroom);
                        await _dbContext.SaveChangesAsync();

                        var requestUser = new UserChatRoom { UserId = friendship.RequesterId, ChatRoomId = chatroom.Id};
                        var receivedUser = new UserChatRoom { UserId = friendship.ReceiverId, ChatRoomId = chatroom.Id};

                        await _dbContext.UserChatRooms.AddRangeAsync(requestUser,receivedUser);
                        await _dbContext.SaveChangesAsync();

                        return Ok( new ApiResponse<string>("已接受好友請求") );
                    case "reject":
                        friendship.Status = FriendshipState.Rejected;
                        friendship.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();
                        return Ok( new ApiResponse<string>(new List<string> { "好友請求被拒絕" }) );
                    default:
                        return BadRequest( new ApiResponse<string>(new List<string> { "無效操作" }) );
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }


        }


        [Authorize]
        [HttpPatch]
        public async Task<IActionResult> UpdateFriendStatus([FromBody] FriendStatusUpdateDTO update)
        {
            var friendship = await _dbContext.Friendships.FindAsync(update.FriendshipId);
            if (friendship == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "好友關係不存在" }) );
            }

            // 更新好友關係的狀態
            switch (update.Action.ToLower())
            {
                case "reject":
                    friendship.Status = FriendshipState.Rejected;
                    friendship.UpdatedAt = DateTime.UtcNow;

                    var roomToUpdate = await _dbContext.ChatRooms.Where(cr => cr.FriendshipId == friendship.Id).FirstOrDefaultAsync();
                    if(roomToUpdate is not null)
                    {
                        roomToUpdate.IsDeleted = true;
                    }
                    break;
                default:
                    return BadRequest( new ApiResponse<string>(new List<string> { "無效操作" }) );
            }

            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>( "狀態已更新" ) );
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }
    }
};
