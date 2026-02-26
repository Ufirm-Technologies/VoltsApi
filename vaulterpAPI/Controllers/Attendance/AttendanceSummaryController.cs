using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Globalization;
using vaulterpAPI.Models.Attendance;

namespace vaulterpAPI.Controllers.Attendance
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceSummaryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AttendanceSummaryController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        [HttpGet("log")]
        public async Task<IActionResult> GetMonthlyAttendanceLogByOffice(int officeId, string monthYear)
        {
            var summary = new List<AttendanceSummaryDto>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            // Step 1: Get all employee emails for given officeId
            var employeeEmails = new List<string>();
            using (var cmd = new NpgsqlCommand(@"
        SELECT DISTINCT email
        FROM master.employee_master
        WHERE office_id = @officeId AND email IS NOT NULL", conn))
            {
                cmd.Parameters.AddWithValue("@officeId", officeId);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    employeeEmails.Add(reader.GetString(0));
                }
            }

            foreach (var email in employeeEmails)
            {
                using var tran = await conn.BeginTransactionAsync();

                using (var cmd = new NpgsqlCommand("CALL getmonthlyattendancelog(@p_mobileno, @p_selectedmonth, @ref)", conn, (NpgsqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("p_mobileno", email);
                    cmd.Parameters.AddWithValue("p_selectedmonth", monthYear);
                    cmd.Parameters.Add(new NpgsqlParameter("ref", NpgsqlTypes.NpgsqlDbType.Refcursor)
                    {
                        Direction = System.Data.ParameterDirection.InputOutput,
                        Value = "my_cursor"
                    });

                    await cmd.ExecuteNonQueryAsync();
                }

                using (var fetchCmd = new NpgsqlCommand("FETCH ALL IN \"my_cursor\";", conn, (NpgsqlTransaction)tran))
                using (var reader = await fetchCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        summary.Add(new AttendanceSummaryDto
                        {
                            PunchDate = reader.GetDateTime(reader.GetOrdinal("punchdate")),
                            EmployeeName = reader.GetString(reader.GetOrdinal("employeename")),
                            DayOfWeek = reader.GetDateTime(reader.GetOrdinal("punchdate")).ToString("dddd"),
                            MinCheckIn = reader.IsDBNull(reader.GetOrdinal("mincheckin")) ? null : reader.GetString(reader.GetOrdinal("mincheckin")),
                            MaxCheckOut = reader.IsDBNull(reader.GetOrdinal("maxcheckout")) ? null : reader.GetString(reader.GetOrdinal("maxcheckout")),
                            TotalWorkingTime = reader.IsDBNull(reader.GetOrdinal("totalworkingtime")) ? null : reader.GetString(reader.GetOrdinal("totalworkingtime")),
                            Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : reader.GetString(reader.GetOrdinal("status"))
                        });
                    }
                }

                await tran.CommitAsync();
            }

            return Ok(summary.OrderBy(x => x.PunchDate).ThenBy(x => x.EmployeeName));
        }

        [HttpGet("status-summary")]
        public async Task<IActionResult> GetAttendanceStatusSummary(int officeId, string monthYear, string? department = null)
        {
            int present = 0, absent = 0, leave = 0;

            using var conn = GetConnection();
            await conn.OpenAsync();

            // Step 1: Get all employee emails for given officeId (and optionally department)
            var employeeEmails = new List<string>();
            var query = @"
        SELECT DISTINCT email
        FROM master.employee_master
        WHERE office_id = @officeId AND email IS NOT NULL";

            if (!string.IsNullOrEmpty(department))
                query += " AND department = @department"; // filter by department name

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@officeId", officeId);
                if (!string.IsNullOrEmpty(department))
                    cmd.Parameters.AddWithValue("@department", department);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    employeeEmails.Add(reader.GetString(0));
                }
            }

            // Step 2: Loop through employees, call procedure, and count statuses
            foreach (var email in employeeEmails)
            {
                using var tran = await conn.BeginTransactionAsync();

                var cursorName = $"cursor_{Guid.NewGuid():N}";

                using (var cmd = new NpgsqlCommand("CALL getmonthlyattendancelog(@p_mobileno, @p_selectedmonth, @ref)", conn, (NpgsqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("p_mobileno", email);
                    cmd.Parameters.AddWithValue("p_selectedmonth", monthYear);
                    cmd.Parameters.Add(new NpgsqlParameter("ref", NpgsqlTypes.NpgsqlDbType.Refcursor)
                    {
                        Direction = System.Data.ParameterDirection.InputOutput,
                        Value = cursorName
                    });

                    await cmd.ExecuteNonQueryAsync();
                }

                using (var fetchCmd = new NpgsqlCommand($"FETCH ALL IN \"{cursorName}\";", conn, (NpgsqlTransaction)tran))
                using (var reader = await fetchCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var status = reader.GetString(reader.GetOrdinal("totalworkingtime"));

                        if (TimeSpan.TryParse(status, out _))
                            present++;
                        else
                            switch (status.ToLower())
                            {
                                case "absent": absent++; break;
                                case "leave": leave++; break;
                            }
                    }
                }

                await tran.CommitAsync();
            }

            var summaryResult = new
            {
                OfficeId = officeId,
                Department = department,
                MonthYear = monthYear,
                PresentCount = present,
                AbsentCount = absent,
                LeaveCount = leave
            };

            return Ok(summaryResult);
        }
    }
}
