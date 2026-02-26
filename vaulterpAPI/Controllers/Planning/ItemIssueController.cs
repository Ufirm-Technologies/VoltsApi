using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Planning;

namespace vaulterpAPI.Controllers.Planning
{
    [ApiController]
    [Route("api/planning/[controller]")]
    public class ItemIssueController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public ItemIssueController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ GET all by officeId
        [HttpGet]
        public async Task<IActionResult> GetAllByOffice([FromQuery] int officeId)
        {
            var list = new List<ItemIssueDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"SELECT * FROM planning.item_issue 
                          WHERE office_id = @office_id AND is_active = TRUE";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ItemIssueDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Inwo = reader.GetInt32(reader.GetOrdinal("inwo")),
                    JobcardId = reader.IsDBNull(reader.GetOrdinal("jobcard_id"))
                                    ? null : reader.GetInt32(reader.GetOrdinal("jobcard_id")),
                    Operation = reader.GetString(reader.GetOrdinal("operation")),
                    EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                    ItemId = reader.GetInt32(reader.GetOrdinal("item_id")),
                    QuantityIssued = reader.GetInt32(reader.GetOrdinal("quantity_issued")),
                    CreatedBy = reader.GetInt32(reader.GetOrdinal("created_by")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("created_on")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    OfficeId = reader.GetInt32(reader.GetOrdinal("office_id"))
                });
            }

            return Ok(list);
        }

        // ✅ POST (insert new)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ItemIssueDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                INSERT INTO planning.item_issue
                (inwo, jobcard_id, operation, employee_id, item_id, quantity_issued, created_by, created_on, is_active, office_id)
                VALUES
                (@inwo, @jobcard_id, @operation, @employee_id, @item_id, @quantity_issued, @created_by, NOW(), TRUE, @office_id)
                RETURNING id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@inwo", dto.Inwo);
            cmd.Parameters.AddWithValue("@jobcard_id", (object?)dto.JobcardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@operation", dto.Operation ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("@item_id", dto.ItemId);
            cmd.Parameters.AddWithValue("@quantity_issued", dto.QuantityIssued);
            cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);

            await conn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();

            return Ok(new { message = "Inserted successfully", id });
        }

        // ✅ PUT (update by Id)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ItemIssueDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                UPDATE planning.item_issue
                SET inwo = @inwo,
                    jobcard_id = @jobcard_id,
                    operation = @operation,
                    employee_id = @employee_id,
                    item_id = @item_id,
                    quantity_issued = @quantity_issued,
                    office_id = @office_id,
                    is_active = @is_active
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@inwo", dto.Inwo);
            cmd.Parameters.AddWithValue("@jobcard_id", (object?)dto.JobcardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@operation", dto.Operation ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("@item_id", dto.ItemId);
            cmd.Parameters.AddWithValue("@quantity_issued", dto.QuantityIssued);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("@is_active", dto.IsActive);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0) return NotFound();
            return Ok(new { message = "Updated successfully" });
        }

        // ✅ DELETE (soft delete by Id)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"UPDATE planning.item_issue SET is_active = FALSE WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0) return NotFound();
            return Ok(new { message = "Deleted successfully" });
        }
    }
}
