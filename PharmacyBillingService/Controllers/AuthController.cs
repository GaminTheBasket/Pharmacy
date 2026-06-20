using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PharmacyBillingService.Data;
using PharmacyBillingService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PharmacyBillingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly PharmacyDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(PharmacyDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = "Patient";
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username && u.Password == request.Password);
            if (user == null)
            {
                return Unauthorized("Sai tài khoản hoặc mật khẩu");
            }

            // 2. Tạo Claims (Dữ liệu nhúng vào Token)
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // 3. Ký Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2), // Token sống 2 tiếng
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                username = user.Username,
                role = user.Role
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            if (_context.Users.Any(u => u.Username == request.Username))
            {
                return BadRequest("Username already exists.");
            }

            var role = string.IsNullOrWhiteSpace(request.Role) ? "Patient" : request.Role.Trim();
            var allowedRoles = new[] { "Admin", "Doctor", "Nurse", "Patient" };
            if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid role.");
            }

            if (!role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return Unauthorized("Admin authentication required to create non-patient accounts.");
                }

                if (!User.IsInRole("Admin"))
                {
                    return Forbid("Only admin can create doctor, nurse, or admin accounts.");
                }
            }

            var user = new User
            {
                Username = request.Username,
                Password = request.Password,
                Role = role
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Created(string.Empty, new { user.Id, user.Username, user.Role });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized();
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new { user.Id, user.Username, user.Role });
        }
    }
}