using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using vaulterpAPI.Models.Identity;

namespace vaulterpAPI.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                SELECT u.*, e.employee_name
                FROM identity.user u
                JOIN master.employee_master e ON u.employee_id = e.employee_id
                WHERE LOWER(u.email) = LOWER(@email) AND u.is_active = TRUE", conn);

            cmd.Parameters.AddWithValue("@email", login.Email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Unauthorized(new { message = "Invalid credentials" });

            var storedHash = reader["password_hash"].ToString();
            if (!BCrypt.Net.BCrypt.Verify(login.Password, storedHash))
                return Unauthorized(new { message = "Invalid credentials" });

            // Parse allowed pages correctly
            var allowedPagesRaw = reader["allowedpages"]?.ToString() ?? "";
            string[] allowedPages = allowedPagesRaw
                .Trim('{', '}')
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim('"')) // remove any quotes
                .ToArray();

            var userDto = new UserDto
            {
                Id = (int)reader["id"],
                Email = reader["email"]?.ToString(),
                PasswordHash = null, // never return password hash
                UsertypeId = (int)reader["usertype_id"],
                EmployeeId = (int)reader["employee_id"],
                AllowedPages = reader["allowedpages"] as string[] ?? Array.Empty<string>(),
                OfficeId = reader["office_id"] as int?,
                IsFirstLogin = (bool)reader["is_first_login"],
                IsActive = (bool)reader["is_active"],
                LastLogin = reader["last_login"] as DateTime?,
                CreatedOn = (DateTime)reader["created_on"]
            };

            // Create JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userDto.Id.ToString()),
                    new Claim(ClaimTypes.Email, userDto.Email ?? ""),
                    new Claim(ClaimTypes.Role, userDto.UsertypeId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(4),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new
            {
                token = jwt,
                user = userDto
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
