using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ChatAPI.Data;
using ChatAPI.DTO;
using ChatAPI.DTO.User;
using ChatAPI.Entities;
using ChatAPI.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static ChatAPI.Extensions.ExceptionMiddleware;

namespace ChatAPI.Endpoints
{
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly ChatAPIContext _dbContext;

        public AuthController(ChatAPIContext dbContext)
        {
            _dbContext = dbContext;
        }

        //註冊
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDTO newUser)
        {

            if (!ModelState.IsValid)
            {
                // 如果 ModelState 失敗，將錯誤進行包裝
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage)
                                                .ToList();

                return BadRequest(new ApiResponse<string>(errors));
            }

            if (_dbContext.Users.Any(u => u.Username == newUser.Username || u.Email == newUser.Email || u.Phone == newUser.Phone))
            {
                return BadRequest( new ApiResponse<string>(new List<string> { "使用者名稱、信箱或手機已被使用" }) );
            }

            var user = newUser.ToEntity();
            
            // 使用 PasswordHasher 來 Hash 密碼
            var passwordHasher = new PasswordHasher<User>();
            user.Password = passwordHasher.HashPassword(user, newUser.Password); // 自動加 Salt 並多次 Hash

            _dbContext.Users.Add(user);

            try
            {
                await _dbContext.SaveChangesAsync();
                return Ok( new ApiResponse<string>("註冊成功!") );
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<string>(new List<string> { "伺服器發生錯誤，請稍後再試" }));
            }
        }

        //登入
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginAuthUserDTO dto)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.InputString || u.Phone == dto.InputString);
            if (user == null)
                return Unauthorized( new ApiResponse<string>(new List<string> { "信箱或手機不存在" }) );

            var passwordHasher = new PasswordHasher<User>();
            
            // 驗證密碼
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized( new ApiResponse<string>(new List<string> { "(信箱/手機)或密碼不正確" }) );

            var token = new LoginResponseDTO {
                Token = GenerateJwtToken(user)
            };

            return Ok( new ApiResponse<LoginResponseDTO>(token) );
        }


        //驗證token
        [Authorize]
        [HttpGet("validate-token")]
        public IActionResult ValidateToken()
        {
            return Ok(new { valid = true });
        }

        public static string GenerateJwtToken(User user)
        {
            // 定義密鑰和加密算法
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("s3cr3t_k3y_!@#_$tr0ng_AND_R@nd0m")); // 替換為更安全的密鑰
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 定義JWT（claims）
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Token ID
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // 使用者ID
            };

            // 創建Token對象
            var token = new JwtSecurityToken(
                issuer: "https://chat-web-app-backend-render.onrender.com",  // 後端
                audience: "https://chat-web-app-vercel.vercel.app",          // 前端
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            // 生成並返回JWT
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


