using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Attendance;

namespace vaulterpAPI.Controllers.Attendance
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LeaveRequestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // ✅ GET: api/LeaveRequest/get-leaves/office/101
        [HttpGet("get-leaves/office/{officeId}")]
        public async Task<IActionResult> GetLeavesByOffice(int officeId)
        {
            var leaves = new List<LeaveRequestDTO>();
            var query = @"
                SELECT 
                    lr.leave_id, 
                    lr.mobile_no, 
                    lr.from_date, 
                    lr.to_date, 
                    lr.reason, 
                    lr.status, 
                    lr.applied_on, 
                    lr.leave_type, 
                    lr.employee_id,
                    lr.rejection_remarks,
                    lr.leave_type_id,
                    lr.leave_count,
                    e.employee_name,
                    e.office_id
                FROM attendance.leave_request lr
                LEFT JOIN master.employee_master e 
                    ON lr.employee_id = e.employee_id
                WHERE e.office_id = @OfficeId";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@OfficeId", officeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                leaves.Add(new LeaveRequestDTO
                {
                    LeaveId = reader.GetInt32(0),
                    MobileNo = reader.IsDBNull(1) ? null : reader.GetString(1),
                    FromDate = reader.GetDateTime(2),
                    ToDate = reader.GetDateTime(3),
                    Reason = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Status = reader.GetString(5),
                    AppliedOn = reader.GetDateTime(6),
                    LeaveType = reader.IsDBNull(7) ? null : reader.GetString(7),
                    EmployeeId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                    RejectionRemarks = reader.IsDBNull(9) ? null : reader.GetString(9),
                    LeaveTypeId = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                    LeaveCount = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11),
                    EmployeeName = reader.IsDBNull(12) ? null : reader.GetString(12),
                    OfficeId = reader.IsDBNull(13) ? 0 : reader.GetInt32(13)
                });
            }

            return Ok(leaves);
        }

        // ✅ POST: api/LeaveRequest/apply-leave
        [HttpPost("apply-leave")]
        public async Task<IActionResult> ApplyLeave([FromBody] LeaveRequestCreateModel model)
        {
            var query = @"
                INSERT INTO attendance.leave_request
                (mobile_no, from_date, to_date, reason, status, applied_on, leave_type, employee_id, leave_type_id, leave_count)
                VALUES (@MobileNo, @FromDate, @ToDate, @Reason, 'Pending', NOW(), @LeaveType, @EmployeeId, @LeaveTypeId, @LeaveCount)
                RETURNING leave_id";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FromDate", model.FromDate);
            cmd.Parameters.AddWithValue("@ToDate", model.ToDate);
            cmd.Parameters.AddWithValue("@Reason", model.Reason ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LeaveType", model.LeaveType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EmployeeId", model.EmployeeId);
            cmd.Parameters.AddWithValue("@LeaveTypeId", model.LeaveTypeId);
            cmd.Parameters.AddWithValue("@LeaveCount", model.LeaveCount);

            var leaveId = await cmd.ExecuteScalarAsync();

            return Ok(new { Message = "Leave applied successfully.", LeaveId = leaveId });
        }

        // ✅ PUT: api/LeaveRequest/approve-leave/1
        [HttpPut("approve-leave/{leaveId}")]
        public async Task<IActionResult> ApproveLeave(int leaveId)
        {
            var query = @"UPDATE attendance.leave_request 
                          SET status = 'Approved' 
                          WHERE leave_id = @LeaveId AND status = 'Pending'";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@LeaveId", leaveId);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
                return Ok(new { Message = "Leave approved successfully." });
            else
                return NotFound(new { Message = "Leave not found or already processed." });
        }

        // ✅ PUT: api/LeaveRequest/reject-leave/1
        [HttpPut("reject-leave/{leaveId}")]
        public async Task<IActionResult> RejectLeave(int leaveId, [FromBody] RejectLeaveModel model)
        {
            var query = @"UPDATE attendance.leave_request 
                          SET status = 'Rejected', rejection_remarks = @Remarks 
                          WHERE leave_id = @LeaveId AND status = 'Pending'";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@LeaveId", leaveId);
            cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
                return Ok(new { Message = "Leave rejected successfully." });
            else
                return NotFound(new { Message = "Leave not found or already processed." });
        }

        // ✅ GET: api/LeaveRequest/leave-balance/email/priya.shah@example.com
        [HttpGet("leave-balance/email/{email}")]
        public async Task<IActionResult> GetLeaveBalanceByEmail(string email)
        {
            var balances = new List<LeaveBalanceDTO>();
            var query = @"
        SELECT 
            el.leave_type_id,
            lm.leave_type,
            COALESCE(SUM(CASE WHEN lr.status = 'Approved' THEN lr.leave_count ELSE 0 END), 0) AS TakenLeaves,
            (el.balance - COALESCE(SUM(CASE WHEN lr.status = 'Approved' THEN lr.leave_count ELSE 0 END), 0)) AS RemainingLeaves
        FROM attendance.employee_leave el
        INNER JOIN master.employee_master em ON el.employee_id = em.employee_id
        INNER JOIN attendance.leave_master lm ON el.leave_type_id = lm.id
        LEFT JOIN attendance.leave_request lr 
            ON lr.employee_id = em.employee_id 
            AND lr.leave_type_id = el.leave_type_id
        WHERE em.email = @Email
        GROUP BY el.leave_type_id, lm.leave_type, el.balance";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                balances.Add(new LeaveBalanceDTO
                {
                    LeaveTypeId = reader.GetInt32(0),
                    LeaveType = reader.GetString(1),
                    TakenLeaves = reader.GetDecimal(2),
                    RemainingLeaves = reader.GetDecimal(3)
                });
            }

            return Ok(balances);
        }

    }
}
