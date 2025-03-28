using Microsoft.AspNetCore.Mvc;
using SchedulediaryApi.DTOs;
using SchedulediaryApi.Models;
using SchedulediaryApi.Services;
using SchedulediaryApi.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SchedulediaryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserRepository _userRepo;
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _configuration;

        public UsersController(UserRepository userRepo, ILogger<UsersController> logger, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) ||
                string.IsNullOrWhiteSpace(dto.Account) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.Email))
            {
                _logger.LogWarning("註冊失敗：欄位未填寫完整");
                return BadRequest("請填寫所有必填欄位");
            }

            if (dto.Password != dto.ConfirmPassword)
            {
                _logger.LogWarning("註冊失敗：密碼不一致，Account={Account}", dto.Account);
                return BadRequest("兩次輸入的密碼不一致");
            }

            if (_userRepo.IsAccountExist(dto.Account))
            {
                _logger.LogWarning("註冊失敗：帳號已存在，Account={Account}", dto.Account);
                return Conflict("帳號已存在");
            }

            // 使用 bcrypt 哈希密碼
            var hash = PasswordHelper.HashPassword(dto.Password);

            var user = new User
            {
                Name = dto.Name,
                Account = dto.Account,
                Email = dto.Email,
                Gender = dto.Gender,
                Birthday = dto.Birthday,
                CreatedAt = DateTime.Now,
                PasswordHash = hash
            };

            _userRepo.Register(user);
            _logger.LogInformation("註冊成功，Account={Account}, Name={Name}", user.Account, user.Name);
            return Ok("註冊成功");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var user = _userRepo.GetUserByAccount(dto.Account);
            if (user == null)
            {
                _logger.LogWarning("登入失敗：帳號不存在，Account={Account}", dto.Account);
                return NotFound("帳號不存在");
            }

            // 使用 bcrypt 驗證密碼
            bool valid = PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash);
            if (!valid)
            {
                _logger.LogWarning("登入失敗：密碼錯誤，Account={Account}", dto.Account);
                return Unauthorized("密碼錯誤");
            }

            var token = GenerateJwtToken(user);
            _logger.LogInformation("登入成功，Account={Account}, UserId={UserId}", user.Account, user.UserId);
            return Ok(new { message = "登入成功", userId = user.UserId, user.Name, token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new("sub", user.UserId.ToString()),
                new(ClaimTypes.Name, user.Name)
            };

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}