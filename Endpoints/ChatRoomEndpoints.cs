using ChatAPI.Data;
using ChatAPI.DTO;
using ChatAPI.Entities;
using ChatAPI.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatRoomController : ControllerBase
    {
        private readonly ChatAPIContext _dbContext;

        public ChatRoomController(ChatAPIContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Get all public chat rooms
        [HttpGet]
        public async Task<IActionResult> GetChatRooms([FromQuery] int? userId, [FromQuery] int? roomtype, [FromQuery] bool? hasjoin)
        {
            var query = _dbContext.ChatRooms
                .Where(r => !r.IsDeleted)
                .Include(r => r.CreatedBy)  // 確保加載 CreatedBy（創建者）用戶資料
                .Include(r => r.UserChatRooms)  // 也保留加載 UserChatRooms
                .ThenInclude(uc => uc.User)  // 如果需要 User 資料，這裡可以加載 User
                .AsQueryable();

            // 如果 userId 有值，則篩選與該使用者相關的房間
            if (userId.HasValue)
            {
                // 如果 roomtype 也有值，篩選與該 userId 和 roomtype 都相關的房間
                if (roomtype.HasValue)
                {
                    if(hasjoin.HasValue){
                        if(hasjoin.Value)
                        {
                            query = query.Where(r => r.UserChatRooms.Any(uc => uc.UserId == userId && uc.IsActive) && r.RoomType == roomtype);                  
                        }else{
                            query = query.Where(r => !r.UserChatRooms.Any(uc => uc.UserId == userId && uc.IsActive) && r.RoomType == roomtype);
                        }
                    }
                    else{
                        query = query.Where(r => r.UserChatRooms.Any(uc => uc.UserId == userId && uc.IsActive) && r.RoomType == roomtype);  
                    }
                }
                else
                {
                    // 如果只有 userId，則只篩選與該 userId 相關的房間
                    query = query.Where(r => r.UserChatRooms.Any(uc => uc.UserId == userId && uc.IsActive));
                }
            }else if(roomtype.HasValue){
                query = query.Where(r => r.RoomType == roomtype);
            }

            var rooms = await query
                .Select(c => c.ToChatroomSummaryDTO())
                .AsNoTracking()
                .ToListAsync();

            return Ok(rooms);
        }

        // Get a specific chat room by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChatRoom(int id)
        {
            var room = await _dbContext.ChatRooms
                .Where(r => r.Id == id && !r.IsDeleted)
                .Include(r => r.UserChatRooms)    // 預先載入 UserChatRooms
                .ThenInclude(uc => uc.User)      // 預先載入 User
                .Select(c => c.ToChatroomDTO())
                .FirstOrDefaultAsync();

            if (room == null)
            {
                return NotFound();
            }

            return Ok(room);
        }

        // Create a new chat room
        [HttpPost]
        public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatroomDTO newChatroom)
        {
            // 確認創建者是否存在
            var creator = await _dbContext.Users.Where(u => u.Id  == newChatroom.CreatedByUserId).FirstOrDefaultAsync();
            if (creator == null)
            {
                return NotFound(new { success = false, message = "創建用戶不存在" });
            }
            // 如果是好友聊天室，確認是否存在好友關係
            int? friendshipId = null;
            if (newChatroom.RoomType == 0) // 如果是好友聊天室
            {

                if (newChatroom.ReceiverFriendshipId != null)
                {
                    var friendship = await _dbContext.Friendships
                        .FirstOrDefaultAsync(f => (f.RequesterId == newChatroom.CreatedByUserId && f.ReceiverId == newChatroom.ReceiverFriendshipId)
                                            || (f.RequesterId == newChatroom.ReceiverFriendshipId && f.ReceiverId == newChatroom.CreatedByUserId));

                    if (friendship == null || friendship.Status != FriendshipState.Accepted)
                    {
                        return BadRequest(new { success = false, message = "兩位用戶之間未建立好友關係或尚未接受" });
                    }

                    friendshipId = friendship.Id; // 將好友關係 ID 連結到聊天室
                }
                else
                {
                    return BadRequest(new { success = false, message = "私人聊天室必須包含至少兩位參與者" });
                }

                var roomExist = await _dbContext.ChatRooms.Where( cr => cr.FriendshipForeignKey == friendshipId).AnyAsync();
                if(roomExist)
                {
                    return BadRequest(new { success = false, message = "已有聊天室" });
                }
            }


            
            // 創建聊天室實體並新增至資料庫
            var room = new ChatRoom
            {
                Roomname = newChatroom.Roomname,
                CreatedByUserId = newChatroom.CreatedByUserId,
                RoomType = newChatroom.RoomType,
                FriendshipForeignKey = friendshipId, 
                IsDeleted = false
            };
            Console.WriteLine(room);
            _dbContext.ChatRooms.Add(room);
            await _dbContext.SaveChangesAsync(); // 確保 `room.Id` 被正確設置

            // 預先初始化 Messages 欄位，防止為空
            room.Messages ??= [];

            // 將創建者加入聊天室成員表
            var userChatRoom = new UserChatRoom
            {
                UserId = newChatroom.CreatedByUserId,
                ChatRoomId = room.Id,
                User = creator,
                ChatRoom = room,
                Role = UserRole.Admin
            };

            _dbContext.UserChatRooms.Add(userChatRoom);

            // 如果是私人聊天室，還需要將另一位參與者加入
            if (friendshipId is not null)
            {
                if (newChatroom.ReceiverFriendshipId != null)
                {
                    var otherUser = await _dbContext.Users.FindAsync(newChatroom.ReceiverFriendshipId);
                    if (otherUser != null)
                    {
                        var userChatRoom2 = new UserChatRoom
                        {
                            UserId = otherUser.Id,
                            ChatRoomId = room.Id,
                            User = otherUser,
                            ChatRoom = room,
                            Role = UserRole.Member
                        };

                        _dbContext.UserChatRooms.Add(userChatRoom2);
                    }
                }
            }

            // 儲存所有更改
            await _dbContext.SaveChangesAsync();

            // 返回結果
            return CreatedAtAction(nameof(GetChatRoom), new { id = room.Id }, room.ToChatroomDTO());
        }


        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinChatRoom(int id, [FromBody] JoinChatroomDTO joinUser)
        {
            var room = await _dbContext.ChatRooms.Include( cr => cr.UserChatRooms).FirstOrDefaultAsync(cr => cr.Id == id);

            if(room is null)
            {
                return NotFound();
            }

            var user = await _dbContext.Users.FindAsync(joinUser.UserId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "用戶不存在" });
            }

            // var isAlreadyInRoom = room.UserChatRooms.Any(uc => uc.UserId == joinUser.UserId);
            // if (isAlreadyInRoom)
            // {
            //     return BadRequest(new { success = false, message = "用戶已在聊天室中" });
            // }

            var isAlreadyInRoom = room.UserChatRooms.Where(uc => uc.UserId == joinUser.UserId).FirstOrDefault();
            
            if(isAlreadyInRoom is null){
                var userChatRoom = new UserChatRoom
                {
                    UserId = joinUser.UserId,
                    ChatRoomId = id,
                    User = user,
                    ChatRoom = room 
                };

                _dbContext.UserChatRooms.Add(userChatRoom);
                await _dbContext.SaveChangesAsync();
                return Ok(new { success = true, message = "用戶已加入聊天室" });
            }
            else if(!isAlreadyInRoom.IsActive)
            {
                isAlreadyInRoom.IsActive = !isAlreadyInRoom.IsActive;
                await _dbContext.SaveChangesAsync();
                return Ok(new { success = true, message = "用戶已加入聊天室" });
            }else{
            return BadRequest(new { success = false, message = "用戶已在聊天室中" });
            }
        }

        [HttpPost("{Id}/kick")]
        public async Task<IActionResult> KickUserFromChatRoom(int Id, [FromBody] KickChatroomDTO kickRequest)
        {

            var requestUserChatRoom = await _dbContext.UserChatRooms
                .FirstOrDefaultAsync(uc => uc.UserId == kickRequest.RequestUserId && uc.ChatRoomId == Id);

            if (requestUserChatRoom == null)
            {
                return NotFound(new { success = false, message = "用戶不在聊天室中" });
            }

            if(requestUserChatRoom.Role != 0)
            {
                return Forbid();
            }

            var kickUserChatRoom = await _dbContext.UserChatRooms
                .FirstOrDefaultAsync(uc => uc.UserId == kickRequest.KickUserId && uc.ChatRoomId == Id);

            if (kickUserChatRoom == null)
            {
                return NotFound(new { success = false, message = "用戶不在聊天室中" });
            }

            // 標記為不活躍並設置踢出時間
            kickUserChatRoom.IsActive = false;
            kickUserChatRoom.KickoutTime = DateTime.UtcNow;
            kickUserChatRoom.IsBanned = true;  // 永久禁止進入聊天室
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "使用者已被踢出並禁止進入聊天室" });
        }

        // Update a chat room using PATCH
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateChatRoom(int id, [FromBody] UpdatePatchChatroom updateRoom)
        {
            var targetRoom = _dbContext.ChatRooms.Find(id);

            if (targetRoom == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updateRoom.Roomname))
            {
                targetRoom.Roomname = updateRoom.Roomname;
            }

            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Soft delete a chat room
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatRoom(int id)
        {
            var targetRoom = await _dbContext.ChatRooms
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (targetRoom == null)
            {
                return NotFound();
            }

            targetRoom.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> LeaveChatRoom(int id, [FromBody] LeaveChatroomDTO leaveUser)
        {
            var room = await _dbContext.ChatRooms.Include( cr => cr.UserChatRooms).Where(cr => cr.Id == id).FirstOrDefaultAsync();
            if (room == null)
            {
                return NotFound(new { success = false, message = "聊天室不存在" });
            }

            var userChatRoom = room.UserChatRooms.FirstOrDefault(uc => uc.UserId == leaveUser.UserId);
            if (userChatRoom == null)
            {
                return BadRequest(new { success = false, message = "用戶不在此聊天室中" });
            }

            userChatRoom.IsActive = false;
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "用戶已離開聊天室" });
        }
    }
}
