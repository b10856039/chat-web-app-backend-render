using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatAPI.Data;
using ChatAPI.DTO;
using ChatAPI.DTO.User;
using ChatAPI.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ChatAPI.Endpoints
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly ChatAPIContext _dbContext;

        public AuthController(ChatAPIContext dbContext)
        {
            _dbContext = dbContext;
        }


        // [HttpPost("register")]
        // public async Task<IActionResult> Register([FromBody] UserDTO dto)
        // {
        //     if (_dbContext.Users.Any(u => u.Username == dto.Username))
        //         return BadRequest("Username already exists.");

        //     using var hmac = new HMACSHA256();
        //     var user = new User
        //     {
        //         Username = dto.Username,
        //         PasswordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)))
        //     };

        //     _dbContext.Users.Add(user);
        //     await _dbContext.SaveChangesAsync();

        //     return Ok("User registered successfully.");
        // }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginAuthUserDTO dto)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.InputString || u.Phone == dto.InputString);
            if (user == null)
                return Unauthorized("Invalid username or password.");

            // using var hmac = new HMACSHA256();
            // var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)));

            // if (computedHash != user.PasswordHash)
            //     return Unauthorized("Invalid username or password.");

            if (dto.Password != user.Password)
                return Unauthorized("Invalid username or password.");


            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        public static string GenerateJwtToken(User user)
        {
            // 定义密钥和加密算法
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("s3cr3t_k3y_!@#_$tr0ng_AND_R@nd0m")); // 替换为更安全的密钥
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 定义JWT的声明（claims）
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username), // 用户名
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Token ID
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // 用户ID
                new Claim(ClaimTypes.Role,((int)user.Role).ToString()), // 权限信息
                new Claim(ClaimTypes.Email,user.Email),
                new Claim("Phone",user.Phone),
                new Claim("State",user.State.ToString())
            };

            // 创建Token对象
            var token = new JwtSecurityToken(
                issuer: "http://localhost:5266",         // 发布者
                audience: "http://localhost:5266",       // 接收者
                claims: claims,                   // 声明
                expires: DateTime.UtcNow.AddHours(1), // Token有效期
                signingCredentials: credentials   // 签名凭证
            );

            // 生成并返回JWT
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


