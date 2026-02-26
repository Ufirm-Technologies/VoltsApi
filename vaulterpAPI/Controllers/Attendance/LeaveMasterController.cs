using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using vaulterpAPI.Models.Attendance;

namespace vaulterpAPI.Controllers.Attendance
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveMasterController : ControllerBase
    {
        private readonly string _connectionString;

        public LeaveMasterController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ✅ GET all records (with optional officeId filter)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveMasterDto>>> GetAll([FromQuery] int? officeId)
        {
            var result = new List<LeaveMasterDto>();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"SELECT id, office_id, leave_type, leave_description, created_on, created_by, 
                             updated_on, updated_by, is_active
                      FROM attendance.leave_master
                      WHERE (@officeId IS NULL OR office_id = @officeId) AND is_active = true
                      ORDER BY id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@officeId", (object?)officeId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new LeaveMasterDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    LeaveType = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LeaveDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedOn = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    CreatedBy = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    UpdatedOn = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    UpdatedBy = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    IsActive = reader.GetBoolean(8)
                });
            }

            return Ok(result);
        }

        // ✅ GET by Id
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveMasterDto>> GetById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"SELECT id, office_id, leave_type, leave_description, created_on, created_by, 
                             updated_on, updated_by, is_active
                      FROM attendance.leave_master WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Ok(new LeaveMasterDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    LeaveType = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LeaveDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedOn = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    CreatedBy = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    UpdatedOn = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    UpdatedBy = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    IsActive = reader.GetBoolean(8)
                });
            }

            return NotFound();
        }

        // ✅ CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LeaveMasterDto dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"INSERT INTO attendance.leave_master 
                      (office_id, leave_type, leave_description, created_on, created_by, is_active) 
                      VALUES (@office_id, @leave_type, @leave_description, CURRENT_TIMESTAMP, @created_by, true)
                      RETURNING id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", (object?)dto.OfficeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@leave_type", (object?)dto.LeaveType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@leave_description", (object?)dto.LeaveDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", (object?)dto.CreatedBy ?? DBNull.Value);

            var newId = (int)await cmd.ExecuteScalarAsync();
            dto.Id = newId;
            dto.CreatedOn = DateTime.UtcNow;
            dto.IsActive = true;

            return CreatedAtAction(nameof(GetById), new { id = newId }, dto);
        }

        // ✅ UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] LeaveMasterDto dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"UPDATE attendance.leave_master 
                      SET office_id=@office_id, leave_type=@leave_type, leave_description=@leave_description, 
                          updated_on=CURRENT_TIMESTAMP, updated_by=@updated_by, is_active=@is_active
                      WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@office_id", (object?)dto.OfficeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@leave_type", (object?)dto.LeaveType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@leave_description", (object?)dto.LeaveDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_active", dto.IsActive);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();

            return NoContent();
        }

        // ✅ DELETE (Soft delete → set is_active=false)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int? updatedBy)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"UPDATE attendance.leave_master 
                      SET is_active=false, updated_on=CURRENT_TIMESTAMP, updated_by=@updated_by 
                      WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@updated_by", (object?)updatedBy ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();

            return NoContent();
        }
    }
}
