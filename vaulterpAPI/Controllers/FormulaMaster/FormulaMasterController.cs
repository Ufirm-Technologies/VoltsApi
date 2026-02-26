using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Payroll;

namespace vaulterpAPI.Controllers.FormulaMasters
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormulaMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FormulaMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ===============================================
        // 1️⃣ GET ALL (Only Active)
        // ===============================================
        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"
                    SELECT * 
                    FROM payroll.formula_master 
                    WHERE isactive = 1
                    ORDER BY id ASC";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                List<FormulaMaster> list = new();

                while (reader.Read())
                {
                    list.Add(new FormulaMaster
                    {
                        Id = reader["id"] as int?,
                        Name = reader["name"]?.ToString(),
                        Formula = reader["formula"]?.ToString(),
                        FixedValue = reader["fixedvalue"] as decimal?,
                        CreatedOn = reader["createdon"] as DateTime?,
                        UpdatedOn = reader["updatedon"] as DateTime?,
                        IsActive = reader["isactive"] as int?,
                        OfficeId = reader["officeid"] as int?
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

                string query = @"SELECT * FROM payroll.formula_master WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return NotFound(new { message = "Record not found" });

                var fm = new FormulaMaster
                {
                    Id = reader["id"] as int?,
                    Name = reader["name"]?.ToString(),
                    Formula = reader["formula"]?.ToString(),
                    FixedValue = reader["fixedvalue"] as decimal?,
                    CreatedOn = reader["createdon"] as DateTime?,
                    UpdatedOn = reader["updatedon"] as DateTime?,
                    IsActive = reader["isactive"] as int?,
                    OfficeId = reader["officeid"] as int?
                };

                return Ok(fm);
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
        public IActionResult Create([FromBody] FormulaMaster dto)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"
                    INSERT INTO payroll.formula_master
                    (name, formula, fixedvalue, createdon, isactive, officeid)
                    VALUES (@name, @formula, @fixedvalue, CURRENT_TIMESTAMP, 1, @officeid)
                    RETURNING id";

                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@name", dto.Name ?? "");
                cmd.Parameters.AddWithValue("@formula", dto.Formula ?? "");
                cmd.Parameters.AddWithValue("@fixedvalue", dto.FixedValue ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@officeid", dto.OfficeId ?? (object)DBNull.Value);

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
        public IActionResult Update(int id, [FromBody] FormulaMaster dto)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"
                    UPDATE payroll.formula_master
                    SET name = @name,
                        formula = @formula,
                        fixedvalue = @fixedvalue,
                        updatedon = CURRENT_TIMESTAMP,
                        officeid = @officeid
                    WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@name", dto.Name ?? "");
                cmd.Parameters.AddWithValue("@formula", dto.Formula ?? "");
                cmd.Parameters.AddWithValue("@fixedvalue", dto.FixedValue ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@officeid", dto.OfficeId ?? (object)DBNull.Value);

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
        // 5️⃣ SOFT DELETE
        // ===============================================
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                conn.Open();

                string query = @"UPDATE payroll.formula_master SET isactive = 0 WHERE id = @id";

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
