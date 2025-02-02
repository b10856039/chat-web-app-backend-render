using ChatAPI.Data;
using ChatAPI.DTO;
using ChatAPI.Endpoints;
using ChatAPI.Entities;
using ChatAPI.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace ChatAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ChatAPIContext _dbContext;

        public UserController(ChatAPIContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Get all users or filter by username or email
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                List<UserDTO> users = await _dbContext.Users
                    .Where(u => u.Phone == query || u.Username == query || u.Email == query && !u.IsDeleted)
                    .Select( u => u.ToUserDTO())
                    .AsNoTracking()
                    .ToListAsync();

                return users == null ? NotFound() : Ok(users);
            }
            else
            {
                var users = await _dbContext.Users
                    .Select(u => u.ToUserDTO())
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(users);
            }
        }

        // Get a user by ID or username
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == id && !u.IsDeleted)
                .Select(u => u.ToUserDTO())
                .FirstOrDefaultAsync();

            return user == null ? NotFound() : Ok(user);
        }

        // Create a new user
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO newUser)
        {
            var user = newUser.ToEntity();
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user.ToUserDTO());
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
                return NotFound();
            }

            _dbContext.Entry(targetUser).CurrentValues.SetValues(updateUser.ToEntity(id));
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // Update an existing user with PATCH
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchUser(int id, [FromBody] UpdatePatchUserDTO updateUser)
        {
            var targetUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (targetUser == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updateUser.Username)) targetUser.Username = updateUser.Username;
            if (!string.IsNullOrEmpty(updateUser.Email)) targetUser.Email = updateUser.Email;
            if (!string.IsNullOrEmpty(updateUser.Phone)) targetUser.Phone = updateUser.Phone;
            if (updateUser.State.HasValue) targetUser.State = updateUser.State.Value;

            if(!string.IsNullOrEmpty(updateUser.NewPassword) && !string.IsNullOrEmpty(updateUser.OldPassword))
            {
                if(updateUser.OldPassword == targetUser.Password){
                    targetUser.Password = updateUser.NewPassword;
                }else{
                    return Unauthorized("密碼不正確");
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
                catch (FormatException ex)
                {
                    return BadRequest("圖片格式錯誤，請確認 Base64 字串正確。" + ex.Message);
                }
            }

            if (updateUser.Role.HasValue) targetUser.Role = updateUser.Role.Value;

            await _dbContext.SaveChangesAsync();
            


            // 重新生成 Token
            var newToken = AuthController.GenerateJwtToken(targetUser); // 生成新的 JWT token

            // 返回新的 Token 给前端
            return Ok(new { token = newToken }); // 返回新的 token

        }

        // Soft delete a user
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var targetUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (targetUser == null)
            {
                return NotFound();
            }

            targetUser.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
