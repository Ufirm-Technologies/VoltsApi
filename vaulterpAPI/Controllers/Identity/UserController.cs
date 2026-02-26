using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Identity;

namespace vaulterpAPI.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpGet]
        public IActionResult GetAll([FromQuery] int? officeId)
        {
            try
            {
                List<UserDto> users = new();
                string query = "SELECT * FROM identity.user WHERE is_active = true";

                if (officeId.HasValue)
                    query += " AND office_id = @office_id";

                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(query, conn);

                if (officeId.HasValue)
                    cmd.Parameters.AddWithValue("@office_id", officeId.Value);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new UserDto
                    {
                        Id = (int)reader["id"],
                        Email = reader["email"]?.ToString(),
                        UsertypeId = (int)reader["usertype_id"],
                        EmployeeId = (int)reader["employee_id"],
                        OfficeId = reader["office_id"] as int?,
                        IsFirstLogin = (bool)reader["is_first_login"],
                        IsActive = (bool)reader["is_active"],
                        LastLogin = reader["last_login"] as DateTime?,
                        CreatedOn = (DateTime)reader["created_on"]
                    });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching users", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Create(UserDto user)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO identity.user
                    (email, password_hash, usertype_id, employee_id, office_id, is_first_login, is_active, created_on)
                    VALUES
                    (@email, @password_hash, @usertype_id, @employee_id, @office_id, @is_first_login, @is_active, NOW())", conn);

                cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash ?? "");
                cmd.Parameters.AddWithValue("@usertype_id", user.UsertypeId);
                cmd.Parameters.AddWithValue("@employee_id", user.EmployeeId);
                cmd.Parameters.AddWithValue("@office_id", user.OfficeId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@is_first_login", user.IsFirstLogin);
                cmd.Parameters.AddWithValue("@is_active", user.IsActive);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { message = "User created successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating user", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UserDto user)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    UPDATE identity.user SET 
                        email = @email,
                        password_hash = @password_hash,
                        usertype_id = @usertype_id,
                        employee_id = @employee_id,
                        office_id = @office_id,
                        is_first_login = @is_first_login,
                        is_active = @is_active
                    WHERE id = @id", conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash ?? "");
                cmd.Parameters.AddWithValue("@usertype_id", user.UsertypeId);
                cmd.Parameters.AddWithValue("@employee_id", user.EmployeeId);
                cmd.Parameters.AddWithValue("@office_id", user.OfficeId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@is_first_login", user.IsFirstLogin);
                cmd.Parameters.AddWithValue("@is_active", user.IsActive);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { message = "User updated successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating user", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand("UPDATE identity.user SET is_active = false WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { message = "User deleted (soft) successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
            }
        }

        [HttpPut("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            try
            {
                string newPassword = "123456";
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
            UPDATE identity.user 
            SET password_hash = @password_hash, is_first_login = true 
            WHERE LOWER(email) = LOWER(@Email) AND is_active = true", conn);

                cmd.Parameters.AddWithValue("@password_hash", hashedPassword);
                cmd.Parameters.AddWithValue("@Email", request.Email);

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Password reset to default successfully.",
                        email = request.Email,
                        newPassword = newPassword
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No active user found with the provided email."
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error resetting password", error = ex.Message });
            }
        }

    }
}
