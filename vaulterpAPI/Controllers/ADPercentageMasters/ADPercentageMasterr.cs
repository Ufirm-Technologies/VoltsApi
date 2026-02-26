using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Payroll;

namespace vaulterpAPI.Controllers.ADPercentageMasters
{
    [ApiController]
    [Route("api/[controller]")]
    public class ADPercentageMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ADPercentageMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ========================================================
        // 1️⃣ GET ALL (only active = 1)
        // ========================================================
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"
                    SELECT * 
                    FROM payroll.ad_percentage_master 
                    WHERE isactive = 1
                    ORDER BY id ASC";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                List<ADPercentageMaster> list = new();

                while (reader.Read())
                {
                    list.Add(new ADPercentageMaster
                    {
                        Id = reader["id"] as int?,
                        AD_Name = reader["ad_name"]?.ToString(),
                        Percentage = reader["percentage"] as decimal?,
                        IsActive = reader["isactive"] as int?,
                        CreatedOn = reader["createdon"] as DateTime?,
                        UpdatedOn = reader["updatedon"] as DateTime?
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reading records", error = ex.Message });
            }
        }

        // ========================================================
        // 2️⃣ GET BY ID
        // ========================================================
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"SELECT * FROM payroll.ad_percentage_master WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return NotFound(new { message = "Record not found" });

                var item = new ADPercentageMaster
                {
                    Id = reader["id"] as int?,
                    AD_Name = reader["ad_name"]?.ToString(),
                    Percentage = reader["percentage"] as decimal?,
                    IsActive = reader["isactive"] as int?,
                    CreatedOn = reader["createdon"] as DateTime?,
                    UpdatedOn = reader["updatedon"] as DateTime?
                };

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reading record", error = ex.Message });
            }
        }

        // ========================================================
        // 3️⃣ CREATE (INSERT)
        // ========================================================
        [HttpPost]
        public IActionResult Create([FromBody] ADPercentageMaster dto)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"
                    INSERT INTO payroll.ad_percentage_master
                    (ad_name, percentage, isactive, createdon)
                    VALUES (@ad_name, @percentage, 1, CURRENT_TIMESTAMP)
                    RETURNING id";

                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ad_name", dto.AD_Name ?? "");
                cmd.Parameters.AddWithValue("@percentage", dto.Percentage ?? (object)DBNull.Value);

                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                return Ok(new { message = "Created successfully", id = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating record", error = ex.Message });
            }
        }

        // ========================================================
        // 4️⃣ UPDATE
        // ========================================================
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] ADPercentageMaster dto)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"
                    UPDATE payroll.ad_percentage_master
                    SET ad_name = @ad_name,
                        percentage = @percentage,
                        updatedon = CURRENT_TIMESTAMP
                    WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@ad_name", dto.AD_Name ?? "");
                cmd.Parameters.AddWithValue("@percentage", dto.Percentage ?? (object)DBNull.Value);

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

        // ========================================================
        // 5️⃣ SOFT DELETE (isactive = 0)
        // ========================================================
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"UPDATE payroll.ad_percentage_master SET isactive = 0 WHERE id = @id";

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
