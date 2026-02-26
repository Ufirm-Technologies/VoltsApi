using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Attendance;

namespace vaulterpAPI.Controllers.Attendance
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeLeaveController : ControllerBase
    {
        private readonly string _connectionString;

        public EmployeeLeaveController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("office/{officeId}")]
        public async Task<ActionResult<IEnumerable<EmployeeLeaveDto>>> GetByOfficeId(int officeId)
        {
            var result = new List<EmployeeLeaveDto>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"SELECT id, office_id, employee_id, leave_type_id, balance,
                         financial_year, created_on, created_by, updated_on, updated_by, is_active
                  FROM attendance.employee_leave
                  WHERE office_id = @officeId";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("officeId", officeId);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new EmployeeLeaveDto
                {
                    Id = reader.GetInt64(0),
                    OfficeId = reader.GetInt32(1),
                    EmployeeId = reader.GetInt32(2),
                    LeaveTypeId = reader.GetInt32(3),
                    Balance = reader.GetDecimal(4),
                    FinancialYear = reader.GetInt32(5),
                    CreatedOn = reader.GetDateTime(6),
                    CreatedBy = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    UpdatedOn = reader.GetDateTime(8),
                    UpdatedBy = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    IsActive = reader.GetBoolean(10)
                });
            }

            return Ok(result);
        }

        // GET: api/EmployeeLeave/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeLeaveDto>> GetById(long id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"SELECT id, office_id, employee_id, leave_type_id, balance,
                                 financial_year, created_on, created_by, updated_on, updated_by, is_active
                          FROM attendance.employee_leave
                          WHERE id = @id";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Ok(new EmployeeLeaveDto
                {
                    Id = reader.GetInt64(0),
                    OfficeId = reader.GetInt32(1),
                    EmployeeId = reader.GetInt32(2),
                    LeaveTypeId = reader.GetInt32(3),
                    Balance = reader.GetDecimal(4),
                    FinancialYear = reader.GetInt32(5),
                    CreatedOn = reader.GetDateTime(6),
                    CreatedBy = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    UpdatedOn = reader.GetDateTime(8),
                    UpdatedBy = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    IsActive = reader.GetBoolean(10)
                });
            }
            return NotFound();
        }

        // POST: api/EmployeeLeave
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeLeaveDto dto)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"INSERT INTO attendance.employee_leave
                          (office_id, employee_id, leave_type_id, balance,  financial_year, created_by, updated_by, is_active)
                          VALUES (@office_id, @employee_id, @leave_type_id, @balance, @financial_year, @created_by, @updated_by, @is_active)
                          RETURNING id";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("leave_type_id", dto.LeaveTypeId);
            cmd.Parameters.AddWithValue("balance", dto.Balance);
            cmd.Parameters.AddWithValue("financial_year", dto.FinancialYear);
            cmd.Parameters.AddWithValue("created_by", (object?)dto.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("is_active", dto.IsActive);

            var newId = (long)await cmd.ExecuteScalarAsync();
            return CreatedAtAction(nameof(GetById), new { id = newId }, dto);
        }

        // PUT: api/EmployeeLeave/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] EmployeeLeaveDto dto)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"UPDATE attendance.employee_leave
                          SET office_id=@office_id, employee_id=@employee_id, leave_type_id=@leave_type_id,
                              balance=@balance,, financial_year=@financial_year,
                              updated_on=CURRENT_TIMESTAMP, updated_by=@updated_by, is_active=@is_active
                          WHERE id=@id";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("leave_type_id", dto.LeaveTypeId);
            cmd.Parameters.AddWithValue("balance", dto.Balance);
            cmd.Parameters.AddWithValue("financial_year", dto.FinancialYear);
            cmd.Parameters.AddWithValue("updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("is_active", dto.IsActive);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();
            return NoContent();
        }

        // DELETE: api/EmployeeLeave/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"DELETE FROM attendance.employee_leave WHERE id=@id";
            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return NotFound();
            return NoContent();
        }
    }
}
