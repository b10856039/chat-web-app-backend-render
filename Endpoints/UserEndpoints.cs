using System.Security.Cryptography;
using System.Text;
using ChatAPI.Data;
using ChatAPI.DTO;
using ChatAPI.Endpoints;
using ChatAPI.Entities;
using ChatAPI.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ChatAPI.Extensions.ExceptionMiddleware;


namespace ChatAPI.Controllers
{
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ChatAPIContext _dbContext;

        public UserController(ChatAPIContext dbContext)
        {
            _dbContext = dbContext;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                List<UserDTO> users = await _dbContext.Users
                    .Where(u => u.Phone == query || u.ShowUsername == query || u.Email == query && !u.IsDeleted)
                    .Select( u => u.ToUserDTO())
                    .AsNoTracking()
                    .ToListAsync();

                return users == null ? NotFound( new ApiResponse<string>(new List<string> { "無資料" }) ) : Ok( new ApiResponse<List<UserDTO>>(users));
            }
            else
            {
                var users = await _dbContext.Users
                    .Select(u => u.ToUserDTO())
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(new ApiResponse<List<UserDTO>>(users));
            }
        }

        // Get a user by ID 
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == id && !u.IsDeleted)
                .Select(u => u.ToUserDTO())
                .FirstOrDefaultAsync();

            return user == null ? NotFound( new ApiResponse<string>(new List<string> { "無資料" }) ) : Ok( new ApiResponse<UserDTO>(user));
        }

        // Get a user by username
        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetUsername(string username)
        {
            var user = await _dbContext.Users
                .Where(u => u.Username == username && !u.IsDeleted)
                .Select(u => u.ToUserDTO())
                .FirstOrDefaultAsync();

            return user == null ? NotFound( new ApiResponse<string>(new List<string> { "使用者不存在" }) ) : Ok( new ApiResponse<UserDTO>(user) );
        }

        // Create a new user (DEV)
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO newUser)
        {

            if (!ModelState.IsValid)
            {
                // 如果 ModelState 失敗，將錯誤進行包裝
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage)
                                                .ToList();

                return BadRequest(new ApiResponse<string>(errors));
            }

            if(_dbContext.Users.Any(u => u.Username == newUser.Username && u.Email == newUser.Email && u.Phone == newUser.Phone))
            {
                return BadRequest( new ApiResponse<string>(new List<string> { "使用者名稱、信箱或手機已被使用" }) );
            }

            using var hmac = new HMACSHA256();

            var user = newUser.ToEntity();
            user.Password = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(newUser.Password)));

            _dbContext.Users.Add(user);

            try
            {
                await _dbContext.SaveChangesAsync();
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ApiResponse<UserDTO>(user.ToUserDTO()));
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }

        }

        // Update an existing user with PUT
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UpdatePutUserDTO updateUser)
        {
            var targetUser = await _dbContext.Users
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (targetUser == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "使用者不存在" }) );
            }

            _dbContext.Entry(targetUser).CurrentValues.SetValues(updateUser.ToEntity(id));
            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("使用者資料已修改") );
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        // Update an existing user with PATCH
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchUser(int id, [FromBody] UpdatePatchUserDTO updateUser)
        {

            if (!ModelState.IsValid)
            {
                // 如果 ModelState 失敗，將錯誤進行包裝
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage)
                                                .ToList();

                return BadRequest(new ApiResponse<string>(errors));
            }

            var targetUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (targetUser == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "使用者不存在" }) );
            }

            if (!string.IsNullOrEmpty(updateUser.Username)) targetUser.Username = updateUser.Username;
            if (!string.IsNullOrEmpty(updateUser.ShowUsername)) targetUser.ShowUsername = updateUser.ShowUsername;
            if (!string.IsNullOrEmpty(updateUser.Email)) targetUser.Email = updateUser.Email;
            if (!string.IsNullOrEmpty(updateUser.Phone)) targetUser.Phone = updateUser.Phone;
            if (updateUser.State.HasValue) targetUser.State = updateUser.State.Value;

            if(!string.IsNullOrEmpty(updateUser.NewPassword) && !string.IsNullOrEmpty(updateUser.OldPassword))
            {
                if(updateUser.OldPassword == targetUser.Password){
                    targetUser.Password = updateUser.NewPassword;
                }else{
                    return Unauthorized( new ApiResponse<string>(new List<string> { "密碼不正確" }) );
                }

            }

            // 處理圖片 Base64 字串，去除前綴
            if (!string.IsNullOrEmpty(updateUser.PhotoImg))
            {
                string base64String = updateUser.PhotoImg;

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
                    targetUser.PhotoImg = Convert.FromBase64String(base64String);
                }
                catch (FormatException)
                {
                    return BadRequest( new ApiResponse<string>(new List<string> { "圖片格式錯誤，請確認 Base64 字串正確" }) );
                }
            }

            if (updateUser.Role.HasValue) targetUser.Role = updateUser.Role.Value;

            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("修改成功") );
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        // Soft delete a user
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var targetUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (targetUser == null)
            {
                return NotFound( new ApiResponse<string>(new List<string> { "使用者不存在" }) );
            }

            targetUser.IsDeleted = true;

            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("使用者已刪除") );
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }
    }
}
