using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Attendance;

namespace vaulterpAPI.Controllers.Attendance
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManualAttendanceController : ControllerBase
    {
        private readonly string _connectionString;

        public ManualAttendanceController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/manualattendance/office/5
        [HttpGet("office/{officeId}")]
        public async Task<ActionResult<IEnumerable<ManualAttendanceDto>>> GetByOfficeId(int officeId)
        {
            var result = new List<ManualAttendanceDto>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"SELECT id, employee_id, punch_date, check_in_time, check_out_time, 
                                 gate_no, created_by, created_on, updated_on, updated_by,
                                 mobile_no, emp_id, status, image_file, 
                                 is_approved, is_rejected, rejection_remark, office_id
                          FROM attendance.ManualAttendance
                          WHERE office_id = @officeId";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("officeId", officeId);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(MapReaderToDto(reader));
            }

            return Ok(result);
        }

        // GET: api/manualattendance/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ManualAttendanceDto>> GetById(long id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"SELECT id, employee_id, punch_date, check_in_time, check_out_time, 
                                 gate_no, created_by, created_on, updated_on, updated_by,
                                 mobile_no, emp_id, status, image_file, 
                                 is_approved, is_rejected, rejection_remark, office_id
                          FROM attendance.ManualAttendance
                          WHERE id = @id";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Ok(MapReaderToDto(reader));
            }

            return NotFound();
        }

        // POST: api/manualattendance
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ManualAttendanceDto dto)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"INSERT INTO attendance.ManualAttendance
                  (employee_id, punch_date, check_in_time, check_out_time, gate_no, created_by, 
                   mobile_no, emp_id, status, image_file, is_approved, is_rejected, rejection_remark, office_id, updated_by)
                  VALUES (@employee_id, @punch_date, @check_in_time, @check_out_time, @gate_no, @created_by, 
                          @mobile_no, @emp_id, @status, @image_file, FALSE, FALSE, NULL, @office_id, @updated_by)
                  RETURNING id";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("punch_date", dto.PunchDate);
            cmd.Parameters.AddWithValue("check_in_time", (object?)dto.CheckInTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("check_out_time", (object?)dto.CheckOutTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("gate_no", (object?)dto.GateNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("created_by", (object?)dto.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mobile_no", (object?)dto.MobileNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("emp_id", (object?)dto.EmpId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("status", (object?)dto.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("image_file", (object?)dto.ImageFile ?? DBNull.Value);
            cmd.Parameters.AddWithValue("office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);

            var newId = (int)await cmd.ExecuteScalarAsync();

            // ensure API always returns pending status
            dto.IsApproved = false;
            dto.IsRejected = false;
            dto.RejectionRemark = null;

            return CreatedAtAction(nameof(GetById), new { id = newId }, dto);
        }

        // PUT: api/manualattendance/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ManualAttendanceDto dto)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                if (dto.IsApproved)
                {
                    // 1. Get manual attendance record
                    var selectQuery = @"SELECT employee_id, punch_date, check_in_time, check_out_time, gate_no, created_by, 
                                       created_on, mobile_no, emp_id, status, image_file
                                FROM attendance.ManualAttendance
                                WHERE id=@id";
                    await using (var selectCmd = new NpgsqlCommand(selectQuery, conn, tx))
                    {
                        selectCmd.Parameters.AddWithValue("id", id);

                        await using var reader = await selectCmd.ExecuteReaderAsync();
                        if (!await reader.ReadAsync())
                        {
                            return NotFound();
                        }

                        var employeeId = reader.GetInt32(0);
                        var punchDate = reader.GetDateTime(1);
                        var checkInTime = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                        var checkOutTime = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                        var gateNo = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
                        var createdBy = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
                        var createdOn = reader.GetDateTime(6);
                        var mobileNo = reader.IsDBNull(7) ? null : reader.GetString(7);
                        var empId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8);
                        var status = reader.IsDBNull(9) ? null : reader.GetString(9);
                        var imageFile = reader.IsDBNull(10) ? null : (byte[])reader["image_file"];

                        await reader.CloseAsync();

                        // 2. Insert into main punch log
                        var insertQuery = @"INSERT INTO attendance.attendance_logs
                                    (employee_id, punch_time, punch_type, gate_no, created_by, created_on, mobile_no, emp_id, status, image_file_name)
                                    VALUES (@employee_id, @punch_time, @punch_type, @gate_no, @created_by, @created_on, @mobile_no, @emp_id, @status, @image_file_name)";

                        if (checkInTime.HasValue)
                        {
                            await using var insertIn = new NpgsqlCommand(insertQuery, conn, tx);
                            insertIn.Parameters.AddWithValue("employee_id", employeeId);
                            insertIn.Parameters.AddWithValue("punch_time", checkInTime.Value);
                            insertIn.Parameters.AddWithValue("punch_type", "Check In");
                            insertIn.Parameters.AddWithValue("gate_no", (object?)gateNo ?? DBNull.Value);
                            insertIn.Parameters.AddWithValue("created_by", (object?)createdBy ?? DBNull.Value);
                            insertIn.Parameters.AddWithValue("created_on", createdOn);
                            insertIn.Parameters.AddWithValue("mobile_no", (object?)mobileNo ?? DBNull.Value);
                            insertIn.Parameters.AddWithValue("emp_id", (object?)empId ?? DBNull.Value);
                            insertIn.Parameters.AddWithValue("status", (object?)status ?? DBNull.Value);
                            insertIn.Parameters.AddWithValue("image_file_name", SaveImageAndGetUrl(imageFile)); // helper to save image
                            await insertIn.ExecuteNonQueryAsync();
                        }

                        if (checkOutTime.HasValue)
                        {
                            await using var insertOut = new NpgsqlCommand(insertQuery, conn, tx);
                            insertOut.Parameters.AddWithValue("employee_id", employeeId);
                            insertOut.Parameters.AddWithValue("punch_time", checkOutTime.Value);
                            insertOut.Parameters.AddWithValue("punch_type", "Check Out");
                            insertOut.Parameters.AddWithValue("gate_no", (object?)gateNo ?? DBNull.Value);
                            insertOut.Parameters.AddWithValue("created_by", (object?)createdBy ?? DBNull.Value);
                            insertOut.Parameters.AddWithValue("created_on", createdOn);
                            insertOut.Parameters.AddWithValue("mobile_no", (object?)mobileNo ?? DBNull.Value);
                            insertOut.Parameters.AddWithValue("emp_id", (object?)empId ?? DBNull.Value);
                            insertOut.Parameters.AddWithValue("status", (object?)status ?? DBNull.Value);
                            insertOut.Parameters.AddWithValue("image_file_name", SaveImageAndGetUrl(imageFile));
                            await insertOut.ExecuteNonQueryAsync();
                        }

                        // 3. Delete from manual attendance
                        var deleteQuery = @"DELETE FROM attendance.ManualAttendance WHERE id=@id";
                        await using var deleteCmd = new NpgsqlCommand(deleteQuery, conn, tx);
                        deleteCmd.Parameters.AddWithValue("id", id);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }
                }
                else if (dto.IsRejected)
                {
                    // just update rejection
                    var rejectQuery = @"UPDATE attendance.ManualAttendance
                                SET is_approved=FALSE, is_rejected=TRUE, rejection_remark=@rejection_remark,
                                    updated_on=CURRENT_TIMESTAMP, updated_by=@updated_by
                                WHERE id=@id";

                    await using var rejectCmd = new NpgsqlCommand(rejectQuery, conn, tx);
                    rejectCmd.Parameters.AddWithValue("id", id);
                    rejectCmd.Parameters.AddWithValue("updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);
                    rejectCmd.Parameters.AddWithValue("rejection_remark", (object?)dto.RejectionRemark ?? DBNull.Value);
                    var rows = await rejectCmd.ExecuteNonQueryAsync();
                    if (rows == 0) return NotFound();
                }
                else
                {
                    return BadRequest("Must specify approval or rejection.");
                }

                await tx.CommitAsync();
                return NoContent();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private string SaveImageAndGetUrl(byte[]? imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return string.Empty;

            // Generate unique file name
            var fileName = $"{Guid.NewGuid():N}.jpg";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);

            // Save file
            System.IO.File.WriteAllBytes(filePath, imageBytes);

            // Return public URL (adjust for your server hosting config)
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return $"{baseUrl}/images/{fileName}";
        }


        // 🔹 Helper to map query results to DTO
        private ManualAttendanceDto MapReaderToDto(NpgsqlDataReader reader)
        {
            return new ManualAttendanceDto
            {
                Id = reader.GetInt32(0),
                EmployeeId = reader.GetInt32(1),
                PunchDate = reader.GetDateTime(2),
                CheckInTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                CheckOutTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                GateNo = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                CreatedBy = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                CreatedOn = reader.GetDateTime(7),
                UpdatedOn = reader.GetDateTime(8),
                UpdatedBy = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                MobileNo = reader.IsDBNull(10) ? null : reader.GetString(10),
                EmpId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                Status = reader.IsDBNull(12) ? null : reader.GetString(12),
                ImageFile = reader.IsDBNull(13) ? null : (byte[])reader[13],
                IsApproved = reader.GetBoolean(14),
                IsRejected = reader.GetBoolean(15),
                RejectionRemark = reader.IsDBNull(16) ? null : reader.GetString(16),
                OfficeId = reader.GetInt32(17)
            };
        }
    }
}
