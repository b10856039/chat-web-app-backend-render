using ChatAPI.Data;
using ChatAPI.DTO;
using ChatAPI.Entities;
using ChatAPI.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ChatAPI.Extensions.ExceptionMiddleware;

namespace ChatAPI.Controllers
{
    [Route("api/[controller]")]
    public class ChatRoomController : ControllerBase
    {
        private readonly ChatAPIContext _dbContext;

        public ChatRoomController(ChatAPIContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Get all public chat rooms
        // query userId = 使用者id, roomtype = 聊天室類型, hasjoin = 是否加入聊天室
        [HttpGet]
        public async Task<IActionResult> GetChatRooms([FromQuery] int userId, [FromQuery] int? roomtype, [FromQuery] bool? hasjoin)
        {

            //使用者是否存在
            var user = await _dbContext.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound(new ApiResponse<string>(new List<string> { "用戶不存在" }));
            }


            // 建立查詢
            var query = _dbContext.ChatRooms
                .Where(r => !r.IsDeleted)  // 排除已刪除的聊天室
                .Include(r => r.CreatedBy)
                .Include(r => r.UserChatRooms)
                .Include(r => r.Friendship)
                    .ThenInclude(f => f.Requester)
                .Include(r => r.Friendship)
                    .ThenInclude(f => f.Receiver)
                .AsQueryable();

            // 過濾聊天室類型
            if (roomtype.HasValue)
            {
                query = query.Where(r => r.RoomType == (ChatRoomType)roomtype);

                if (roomtype == (int)ChatRoomType.Private) // 私人聊天室
                {
                    query = query.Where(r =>
                        r.Friendship != null &&
                        r.Friendship.Status == FriendshipState.Accepted &&
                        (r.Friendship.RequesterId == userId || r.Friendship.ReceiverId == userId)
                    );
                }
                else if (roomtype == (int)ChatRoomType.Group && hasjoin.HasValue) // 群組聊天室 & 檢查是否加入
                {
                    if (hasjoin.Value) 
                    {
                        query = query.Where(r => r.UserChatRooms.Any(uc => uc.UserId == userId && uc.IsActive && !uc.IsBanned));
                    }
                    else 
                    {
                        query = query.Where(r => !r.UserChatRooms.Any(uc => uc.UserId == userId && uc.IsActive));
                    }
                }
            }

            // 確保 userId 參與的聊天室
            if (!hasjoin.HasValue || hasjoin.Value) // 只有在未加入聊天室時才跳過這個條件
            {
                query = query.Where(r =>
                    r.UserChatRooms.Any(uc => uc.UserId == userId && uc.IsActive && !uc.IsBanned) || 
                    (r.RoomType == ChatRoomType.Private && r.Friendship != null &&
                    (r.Friendship.RequesterId == userId || r.Friendship.ReceiverId == userId))
                );
            }

            // 執行查詢
            var rooms = await query
                .AsNoTracking()
                .Select(c => c.ToChatroomDTO(userId))
                .ToListAsync();

            // 回傳結果
            return Ok(new ApiResponse<List<ChatroomDTO>>(rooms));
        }


        // Get a specific chat room by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChatRoom(int id, [FromQuery] int userId)
        {
            
            //使用者是否存在
            var user = await _dbContext.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound(new ApiResponse<string>(new List<string> { "用戶不存在" }));
            }

            // 2. 查詢聊天室
            var roomDto = await _dbContext.ChatRooms
                .Where(r => r.Id == id && !r.IsDeleted)
                .Include(r => r.CreatedBy)
                .Include(r => r.UserChatRooms)
                    .ThenInclude(uc => uc.User)
                .Include(r => r.Friendship)
                .Where(r => r.RoomType != ChatRoomType.Private || (r.Friendship != null && r.Friendship.Status == FriendshipState.Accepted))
                .Select(c => c.ToChatroomDTO(userId))
                .FirstOrDefaultAsync();

            // 4. 回傳成功結果
            return Ok(new ApiResponse<ChatroomDTO>(roomDto));
        }


        // Create a new chat room
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatroomDTO newChatroom)
        {
            // 確認創建者是否存在
            var creator = await _dbContext.Users.Where(u => u.Id == newChatroom.CreatedByUserId).FirstOrDefaultAsync();
            if (creator == null)
            {
                return NotFound(new ApiResponse<string>(new List<string> { "創建用戶不存在" }));
            }

            // 禁止創建好友聊天室
            if (newChatroom.RoomType == 0)
            {
                return BadRequest(new ApiResponse<string>(new List<string> { "好友聊天室無此權限" }));
            }

            // 檢查聊天室名稱是否有效
            if (string.IsNullOrWhiteSpace(newChatroom.Roomname))
            {
                return BadRequest(new ApiResponse<string>(new List<string> { "聊天室名稱不能為空" }));
            }

            // 創建聊天室實體
            var room = new ChatRoom
            {
                Roomname = newChatroom.Roomname,
                CreatedByUserId = newChatroom.CreatedByUserId,
                RoomType = (ChatRoomType)newChatroom.RoomType,
                IsDeleted = false
            };

            // 處理 Base64 圖片
            if (!string.IsNullOrEmpty(newChatroom.PhotoImg))
            {
                string base64String = newChatroom.PhotoImg;

                // 去除 Base64 字串中的前綴部分
                if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    int commaIndex = base64String.IndexOf(",", StringComparison.OrdinalIgnoreCase);
                    if (commaIndex >= 0)
                    {
                        base64String = base64String.Substring(commaIndex + 1);
                    }
                }

                // 嘗試轉換 Base64 圖片
                try
                {
                    room.PhotoImg = Convert.FromBase64String(base64String);
                }
                catch (FormatException)
                {
                    return BadRequest(new ApiResponse<string>(new List<string> { "圖片格式錯誤，請確認 Base64 字串是否有效" }));
                }
            }

            _dbContext.ChatRooms.Add(room);

            // 預先初始化 Messages 欄位
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

            // 儲存所有更改
            try
            {
                await _dbContext.SaveChangesAsync();
                // 返回結果
                return Ok(new ApiResponse<string>("聊天室已創建"));
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }
            
    


        [Authorize]
        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinChatRoom(int id, [FromBody] JoinChatroomDTO joinUser)
        {
            var room = await _dbContext.ChatRooms.Where( cr => cr.RoomType != ChatRoomType.Private).Include( cr => cr.UserChatRooms).FirstOrDefaultAsync(cr => cr.Id == id);

            if(room is null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "無此聊天室" }) );
            }

            if(room.RoomType == ChatRoomType.Private){
                return BadRequest( new ApiResponse<string>(new List<string> { "好友聊天室無此權限" }) );
            }

            var user = await _dbContext.Users.FindAsync(joinUser.UserId);
            if (user == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "用戶不存在" }) );
            }

            var isAlreadyInRoom = room.UserChatRooms.Where(uc => uc.UserId == joinUser.UserId).FirstOrDefault();

            try
            {
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
                    return Ok( new ApiResponse<string>("用戶已加入聊天室") );
                }
                else if(!isAlreadyInRoom.IsActive)
                {
                    isAlreadyInRoom.IsActive = !isAlreadyInRoom.IsActive;
                    await _dbContext.SaveChangesAsync();
                    return Ok( new ApiResponse<string>("用戶已重新加入聊天室") );
                }else{
                    return BadRequest( new ApiResponse<string>(new List<string> { "用戶已在聊天室中" }) );
                }
            }
            catch(Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        [Authorize]
        [HttpPost("{Id}/kick")]
        public async Task<IActionResult> KickUserFromChatRoom(int Id, [FromBody] KickChatroomDTO kickRequest)
        {

            var requestRoom = await _dbContext.ChatRooms.Where( cr => cr.RoomType != ChatRoomType.Private).Include( cr => cr.UserChatRooms).FirstOrDefaultAsync(cr => cr.Id == Id);

            if(requestRoom is null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "無此聊天室" }) );
            }

            if(requestRoom.RoomType == ChatRoomType.Private){
                return BadRequest( new ApiResponse<string>(new List<string> { "好友聊天室無此權限" }) );
            }

            var requestUserChatRoom = await _dbContext.UserChatRooms
                .FirstOrDefaultAsync(uc => uc.UserId == kickRequest.RequestUserId && uc.ChatRoomId == Id);

            if (requestUserChatRoom == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "用戶不在聊天室中" }));
            }

            if(requestUserChatRoom.Role != 0)
            {
                return StatusCode(403, new ApiResponse<string>(new List<string> { "用戶無此權限" }));
            }

            var kickUserChatRoom = await _dbContext.UserChatRooms
                .FirstOrDefaultAsync(uc => uc.UserId == kickRequest.KickUserId && uc.ChatRoomId == Id);

            if (kickUserChatRoom == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "用戶不在聊天室中" }) );
            }

            // 標記為不活躍並設置踢出時間
            kickUserChatRoom.IsActive = false;
            kickUserChatRoom.KickoutTime = DateTime.UtcNow;
            kickUserChatRoom.IsBanned = true;  // 永久禁止進入聊天室
            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("使用者已被踢出並禁止進入聊天室") );
            }
            catch(Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        // Update a chat room using PATCH
        [Authorize]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateChatRoom(int id, [FromBody] UpdatePatchChatroom updateRoom)
        {
            var targetRoom = _dbContext.ChatRooms.Find(id);

            if (targetRoom == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "聊天室不存在" }) );
            }

            if(targetRoom.CreatedByUserId != updateRoom.UserId)
            {
                return StatusCode(403, new ApiResponse<string>(new List<string> { "用戶無此權限" }));
            }

            if (!string.IsNullOrEmpty(updateRoom.Roomname))
            {
                targetRoom.Roomname = updateRoom.Roomname;
            }

            // 處理圖片 Base64 字串，去除前綴
            if (!string.IsNullOrEmpty(updateRoom.PhotoImg))
            {
                string base64String = updateRoom.PhotoImg;

                // 去除 Base64 字串中的前綴部分 (例如 "data:image/png;base64," 或 "data:image/jpeg;base64,")
                if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    int commaIndex = base64String.IndexOf(",", StringComparison.OrdinalIgnoreCase);
                    if (commaIndex >= 0)
                    {
                        base64String = base64String.Substring(commaIndex + 1); // 只保留純 Base64 部分
                    }
                }

                // 嘗試將純 Base64 字串轉換為 byte[] 並儲存圖片
                try
                {
                    targetRoom.PhotoImg = Convert.FromBase64String(base64String);
                }
                catch (FormatException)
                {
                    return BadRequest( new ApiResponse<string>(new List<string> { "圖片格式錯誤，請確認 Base64 字串正確" }));
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("聊天室內容已修改") );
            }
            catch (FormatException)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        // Soft delete a chat room
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChatRoom(int id, [FromQuery] int userId)
        {
            // 確認使用者是否存在
            var user = await _dbContext.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound(new ApiResponse<string>(new List<string> { "創建用戶不存在" }));
            }

            var targetRoom = await _dbContext.ChatRooms
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);


            if (targetRoom == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "聊天室不存在" }) );
            }

            if(targetRoom.RoomType == ChatRoomType.Private){
                return BadRequest( new ApiResponse<string>(new List<string> { "好友聊天室無此權限" }) );
            }

            if(targetRoom.CreatedByUserId != userId){
                return StatusCode(403, new ApiResponse<string>(new List<string> { "用戶無此權限" }));
            }

            targetRoom.IsDeleted = true;
            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("聊天室已刪除") );
            }
            catch(FormatException)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        [Authorize]
        [HttpDelete("{id}/leave")]
        public async Task<IActionResult> LeaveChatRoom(int id, [FromBody] LeaveChatroomDTO leaveUser)
        {
            var room = await _dbContext.ChatRooms.Include( cr => cr.UserChatRooms).Where(cr => cr.Id == id).FirstOrDefaultAsync();
            if (room == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "無此聊天室" }));
            }

            if(room.CreatedByUserId == leaveUser.UserId)
            {
                return BadRequest( new ApiResponse<string>(new List<string> { "創建者無法離開聊天室，只能刪除聊天室" }) );
            }

            var userChatRoom = room.UserChatRooms.FirstOrDefault(uc => uc.UserId == leaveUser.UserId);
            if (userChatRoom == null)
            {
                return BadRequest( new ApiResponse<string>(new List<string> { "用戶不在此聊天室中" }) );
            }

            userChatRoom.IsActive = false;
            try{
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("用戶已離開聊天室") );
            }
            catch(FormatException)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }
    }
}
