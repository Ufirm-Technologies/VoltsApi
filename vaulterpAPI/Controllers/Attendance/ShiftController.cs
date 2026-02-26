using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Attendance;

namespace vaulterpAPI.Controllers.Attendance
{
    [ApiController]
    [Route("api/attendance/[controller]")]
    public class ShiftController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ShiftController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // GET all shifts by office
        [HttpGet]
        public async Task<IActionResult> GetAllShifts([FromQuery] int officeId)
        {
            var shifts = new List<ShiftDto>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"SELECT shift_id, shift_name, start_time, end_time, shift_code, office_id
                          FROM attendance.shift_master
                          WHERE office_id = @office_id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                shifts.Add(new ShiftDto
                {
                    ShiftId = reader.GetInt32(0),
                    ShiftName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    StartTime = reader.GetTimeSpan(2),
                    EndTime = reader.GetTimeSpan(3),
                    ShiftCode = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OfficeId = reader.GetInt32(5)
                });
            }

            return Ok(shifts);
        }

        // GET by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"SELECT shift_id, shift_name, start_time, end_time, shift_code, office_id
                          FROM attendance.shift_master WHERE shift_id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var shift = new ShiftDto
                {
                    ShiftId = reader.GetInt32(0),
                    ShiftName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    StartTime = reader.GetTimeSpan(2),
                    EndTime = reader.GetTimeSpan(3),
                    ShiftCode = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OfficeId = reader.GetInt32(5)
                };
                return Ok(shift);
            }

            return NotFound();
        }

        // POST
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShiftDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"INSERT INTO attendance.shift_master 
                          (shift_name, start_time, end_time, shift_code, office_id)
                          VALUES (@shift_name, @start_time, @end_time, @shift_code, @office_id)
                          RETURNING shift_id";
            using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@shift_name", (object?)dto.ShiftName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@start_time", dto.StartTime);
            cmd.Parameters.AddWithValue("@end_time", dto.EndTime);
            cmd.Parameters.AddWithValue("@shift_code", (object?)dto.ShiftCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);

            await conn.OpenAsync();
            var insertedId = await cmd.ExecuteScalarAsync();

            return Ok(new { message = "Shift created", id = insertedId });
        }

        // PUT
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShiftDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"UPDATE attendance.shift_master
                          SET shift_name = @shift_name, start_time = @start_time,
                              end_time = @end_time, shift_code = @shift_code, office_id = @office_id
                          WHERE shift_id = @id";
            using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@shift_name", (object?)dto.ShiftName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@start_time", dto.StartTime);
            cmd.Parameters.AddWithValue("@end_time", dto.EndTime);
            cmd.Parameters.AddWithValue("@shift_code", (object?)dto.ShiftCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();

            return affected > 0 ? Ok(new { message = "Shift updated" }) : NotFound();
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"DELETE FROM attendance.shift_master WHERE shift_id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();

            return affected > 0 ? Ok(new { message = "Shift deleted" }) : NotFound();
        }

    }
}
