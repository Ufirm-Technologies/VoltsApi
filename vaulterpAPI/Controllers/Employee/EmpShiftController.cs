using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Employee;

namespace vaulterpAPI.Controllers.Employee
{
    [ApiController]
    [Route("api/attendance/[controller]")]
    public class EmpShiftController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmpShiftController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // GET: All employee shift mappings by officeId with optional date range
        [HttpGet("by-office")]
        public async Task<IActionResult> GetAllShiftsByOffice(int officeId, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var results = new List<EmployeeShiftDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"SELECT es.employee_id, em.employee_name, es.shift_id, sm.shift_name, 
                                 es.date_from, es.date_to, es.is_active, es.mobile_no
                          FROM attendance.employee_shift es
                          JOIN master.employee_master em ON es.employee_id = em.employee_id
                          JOIN attendance.shift_master sm ON es.shift_id = sm.shift_id
                          WHERE em.office_id = @office_id AND es.is_active = TRUE";

            if (dateFrom.HasValue)
                query += " AND es.date_from >= @date_from";
            if (dateTo.HasValue)
                query += " AND es.date_to <= @date_to";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            if (dateFrom.HasValue) cmd.Parameters.AddWithValue("@date_from", dateFrom.Value);
            if (dateTo.HasValue) cmd.Parameters.AddWithValue("@date_to", dateTo.Value);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new EmployeeShiftDto
                {
                    EmployeeId = reader.GetInt32(0),
                    EmployeeName = reader.GetString(1),
                    ShiftId = reader.GetInt32(2),
                    ShiftName = reader.GetString(3),
                    DateFrom = reader.GetDateTime(4),
                    DateTo = reader.GetDateTime(5),
                    IsActive = reader.GetBoolean(6),
                    MobileNo = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return Ok(results);
        }

        // GET: Shifts of a specific employee
        [HttpGet("by-employee/{employeeId}")]
        public async Task<IActionResult> GetShiftsByEmployee(int employeeId)
        {
            var shifts = new List<EmployeeShiftDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"SELECT es.shift_id, sm.shift_name, es.date_from, es.date_to, es.mobile_no, es.is_active
                          FROM attendance.employee_shift es
                          JOIN attendance.shift_master sm ON es.shift_id = sm.shift_id
                          WHERE es.employee_id = @employee_id AND es.is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@employee_id", employeeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                shifts.Add(new EmployeeShiftDto
                {
                    EmployeeId = employeeId,
                    ShiftId = reader.GetInt32(0),
                    ShiftName = reader.GetString(1),
                    DateFrom = reader.GetDateTime(2),
                    DateTo = reader.GetDateTime(3),
                    MobileNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IsActive = reader.GetBoolean(5)
                });
            }

            return Ok(shifts);
        }

        // POST: Create shift mapping
        [HttpPost]
        public async Task<IActionResult> CreateShiftMapping([FromBody] EmployeeShiftDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"INSERT INTO attendance.employee_shift 
                          (employee_id, shift_id, date_from, date_to, is_active, created_by, created_on, mobile_no)
                          VALUES (@employee_id, @shift_id, @date_from, @date_to, TRUE, @created_by, NOW(), @mobile_no)";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("@shift_id", dto.ShiftId);
            cmd.Parameters.AddWithValue("@date_from", dto.DateFrom);
            cmd.Parameters.AddWithValue("@date_to", dto.DateTo);
            cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);
            cmd.Parameters.AddWithValue("@mobile_no", (object?)dto.MobileNo ?? DBNull.Value);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Shift mapping created successfully" });
        }

        // PUT: Update shift mapping
        [HttpPut("{employeeId}/{shiftId}")]
        public async Task<IActionResult> UpdateShiftMapping(int employeeId, int shiftId, [FromBody] EmployeeShiftDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE attendance.employee_shift
                          SET shift_id = @shift_id, date_from = @date_from, date_to = @date_to, 
                              mobile_no = @mobile_no, is_active = @is_active, updated_by = @updated_by, updated_on = NOW()
                          WHERE employee_id = @employee_id AND shift_id = @shift_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@employee_id", employeeId);
            cmd.Parameters.AddWithValue("@shift_id", dto.ShiftId);
            cmd.Parameters.AddWithValue("@date_from", dto.DateFrom);
            cmd.Parameters.AddWithValue("@date_to", dto.DateTo);
            cmd.Parameters.AddWithValue("@mobile_no", (object?)dto.MobileNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_active", dto.IsActive);
            cmd.Parameters.AddWithValue("@updated_by", dto.UpdatedBy);

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();

            return affected > 0 ? Ok(new { message = "Shift mapping updated" }) : NotFound();
        }

        // DELETE: Soft delete mapping
        [HttpDelete("{employeeId}/{shiftId}")]
        public async Task<IActionResult> DeleteMapping(int employeeId, int shiftId)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE attendance.employee_shift 
                          SET is_active = FALSE
                          WHERE employee_id = @employee_id AND shift_id = @shift_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@employee_id", employeeId);
            cmd.Parameters.AddWithValue("@shift_id", shiftId);

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();

            return affected > 0 ? Ok(new { message = "Mapping deleted (soft)" }) : NotFound();
        }
    }
}
