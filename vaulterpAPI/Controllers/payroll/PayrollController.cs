using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models;
using vaulterpAPI.Models.Payroll;

namespace vaulterpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayrollController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PayrollController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ===============================================
        // 1️⃣ GET ALL
        // ===============================================
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"SELECT * FROM payroll.allowance_dedution WHERE is_active = TRUE ORDER BY id";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                List<Payroll> list = new();

                while (reader.Read())
                {
                    list.Add(new Payroll
                    {
                        Id = reader["id"] as int?,
                        Type = reader["type"]?.ToString(),
                        Name = reader["name"]?.ToString(),
                        OfficeId = reader["office_id"] as int?,
                        CreatedOn = reader["created_on"] as DateTime?,
                        CreatedBy = reader["created_by"] as int?,
                        UpdatedOn = reader["updated_on"] as DateTime?,
                        UpdatedBy = reader["updated_by"] as int?,
                        IsActive = (bool)reader["is_active"]
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching records", error = ex.Message });
            }
        }

        // ===============================================
        // 2️⃣ GET BY ID
        // ===============================================
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var query = @"SELECT * FROM payroll.allowance_dedution WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return NotFound(new { message = "Record not found" });

                Payroll record = new()
                {
                    Id = reader["id"] as int?,
                    Type = reader["type"]?.ToString(),
                    Name = reader["name"]?.ToString(),
                    OfficeId = reader["office_id"] as int?,
                    CreatedOn = reader["created_on"] as DateTime?,
                    CreatedBy = reader["created_by"] as int?,
                    UpdatedOn = reader["updated_on"] as DateTime?,
                    UpdatedBy = reader["updated_by"] as int?,
                    IsActive = (bool)reader["is_active"]
                };

                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching record", error = ex.Message });
            }
        }

        // ===============================================
        // 3️⃣ CREATE (INSERT)
        // ===============================================
        [HttpPost]
        public IActionResult Create([FromBody] Payroll dto)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var query = @"
                    INSERT INTO payroll.allowance_dedution
                    (type, name, office_id, created_on, created_by, is_active)
                    VALUES (@type, @name, @office_id, CURRENT_DATE, @created_by, TRUE)
                    RETURNING id";

                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@type", dto.Type ?? "");
                cmd.Parameters.AddWithValue("@name", dto.Name ?? "");
                cmd.Parameters.AddWithValue("@office_id", dto.OfficeId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy ?? (object)DBNull.Value);

                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                return Ok(new { message = "Created successfully", id = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating record", error = ex.Message });
            }
        }

        // ===============================================
        // 4️⃣ UPDATE
        // ===============================================
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Payroll dto)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var query = @"
                    UPDATE payroll.allowance_dedution
                    SET type = @type,
                        name = @name,
                        office_id = @office_id,
                        updated_on = CURRENT_DATE,
                        updated_by = @updated_by
                    WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@type", dto.Type ?? "");
                cmd.Parameters.AddWithValue("@name", dto.Name ?? "");
                cmd.Parameters.AddWithValue("@office_id", dto.OfficeId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@updated_by", dto.UpdatedBy ?? (object)DBNull.Value);

                int rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    return NotFound(new { message = "Record not found" });

                return Ok(new { message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating record", error = ex.Message });
            }
        }

        // ===============================================
        // 5️⃣ DELETE (SOFT DELETE)
        // ===============================================
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                var query = @"UPDATE payroll.allowance_dedution SET is_active = FALSE WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    return NotFound(new { message = "Record not found" });

                return Ok(new { message = "Deleted successfully (soft delete)" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting record", error = ex.Message });
            }
        }
    }
}
