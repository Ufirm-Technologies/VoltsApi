using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Attendance;

namespace vaulterpAPI.Controllers.Attendance
{
    [Route("api/[controller]")]
    [ApiController]
    public class HolidayController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HolidayController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllHolidays()
        {
            var holidays = new List<HolidayDTO>();

            var query = @"SELECT holiday_date, description FROM attendance.holiday_calendar ORDER BY holiday_date";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                holidays.Add(new HolidayDTO
                {
                    HolidayDate = reader.GetDateTime(0),
                    Description = reader.GetString(1)
                });
            }

            return Ok(holidays);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetByMonthYear([FromQuery] int year, [FromQuery] int month)
        {
            var holidays = new List<HolidayDTO>();

            var query = @"
                SELECT holiday_date, description 
                FROM attendance.holiday_calendar 
                WHERE EXTRACT(YEAR FROM holiday_date) = @Year 
                  AND EXTRACT(MONTH FROM holiday_date) = @Month
                ORDER BY holiday_date";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Year", year);
            cmd.Parameters.AddWithValue("@Month", month);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                holidays.Add(new HolidayDTO
                {
                    HolidayDate = reader.GetDateTime(0),
                    Description = reader.GetString(1)
                });
            }

            return Ok(holidays);
        }
    }
}
