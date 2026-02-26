using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Planning;

namespace vaulterpAPI.Controllers.Planning
{
    [ApiController]
    [Route("api/planning/[controller]")]
    public class DailyPlanningSheetController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public DailyPlanningSheetController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpGet]
        public async Task<IActionResult> GetAllByOffice([FromQuery] int officeId)
        {
            var list = new List<DailyPlanningSheetDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = "SELECT * FROM planning.daily_planning_sheet WHERE office_id = @office_id AND is_active = TRUE";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DailyPlanningSheetDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    OfficeId = reader.GetInt32(reader.GetOrdinal("office_id")),
                    PlanDate = reader.GetDateTime(reader.GetOrdinal("plan_date")),
                    EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                    OperationId = reader.GetInt32(reader.GetOrdinal("operation_id")),
                    AssetId = reader.GetInt32(reader.GetOrdinal("asset_id")),
                    InternalWorkOrderId = reader.GetInt32(reader.GetOrdinal("internal_work_order_id")),
                    ShiftId = reader.GetInt32(reader.GetOrdinal("shift_id")),
                    Manpower = reader.GetInt32(reader.GetOrdinal("manpower")),
                    Target = reader["target"] as int?,
                    Achieved = reader["achieved"] as int?,
                    Backfeed = reader["backfeed"]?.ToString(),
                    Remarks = reader["remarks"]?.ToString(),
                    CreatedBy = reader.GetInt32(reader.GetOrdinal("created_by")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("created_on")),
                    UpdatedBy = reader["updated_by"] as int?,
                    UpdatedOn = reader["updated_on"] as DateTime?,
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
                });
            }

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DailyPlanningSheetDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                INSERT INTO planning.daily_planning_sheet 
                (office_id, plan_date, employee_id, operation_id, asset_id, internal_work_order_id, shift_id, manpower, target, achieved, backfeed, remarks, created_by, created_on)
                VALUES
                (@office_id, @plan_date, @employee_id, @operation_id, @asset_id, @internal_work_order_id, @shift_id, @manpower, @target, @achieved, @backfeed, @remarks, @created_by, NOW())
                RETURNING id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("@plan_date", dto.PlanDate);
            cmd.Parameters.AddWithValue("@employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("@operation_id", dto.OperationId);
            cmd.Parameters.AddWithValue("@asset_id", dto.AssetId);
            cmd.Parameters.AddWithValue("@internal_work_order_id", dto.InternalWorkOrderId);
            cmd.Parameters.AddWithValue("@shift_id", dto.ShiftId);
            cmd.Parameters.AddWithValue("@manpower", dto.Manpower);
            cmd.Parameters.AddWithValue("@target", dto.Target ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@achieved", dto.Achieved ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@backfeed", dto.Backfeed ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@remarks", dto.Remarks ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);

            await conn.OpenAsync();
            var id = await cmd.ExecuteScalarAsync();

            return Ok(new { message = "Planning created successfully", id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DailyPlanningSheetDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                UPDATE planning.daily_planning_sheet SET 
                    plan_date = @plan_date, employee_id = @employee_id, operation_id = @operation_id, asset_id = @asset_id,
                    internal_work_order_id = @internal_work_order_id, shift_id = @shift_id, manpower = @manpower, target = @target, achieved = @achieved,
                    backfeed = @backfeed, remarks = @remarks, updated_by = @updated_by, updated_on = NOW()
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@plan_date", dto.PlanDate);
            cmd.Parameters.AddWithValue("@employee_id", dto.EmployeeId);
            cmd.Parameters.AddWithValue("@operation_id", dto.OperationId);
            cmd.Parameters.AddWithValue("@asset_id", dto.AssetId);
            cmd.Parameters.AddWithValue("@internal_work_order_id", dto.InternalWorkOrderId);
            cmd.Parameters.AddWithValue("@shift_id", dto.ShiftId);
            cmd.Parameters.AddWithValue("@manpower", dto.Manpower);
            cmd.Parameters.AddWithValue("@target", dto.Target ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@achieved", dto.Achieved ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@backfeed", dto.Backfeed ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@remarks", dto.Remarks ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@updated_by", dto.UpdatedBy ?? 0);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0 ? Ok(new { message = "Planning updated successfully" }) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = "UPDATE planning.daily_planning_sheet SET is_active = FALSE WHERE id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0 ? Ok(new { message = "Planning deleted (soft)" }) : NotFound();
        }

        // ✅ GET Target by OfficeId + InternalWorkOrderId + OperationId
        [HttpGet("target")]
        public async Task<IActionResult> GetTarget(
            [FromQuery] int officeId,
            [FromQuery] int internalWorkOrderId,
            [FromQuery] int operationId)
        {
            int? target = null;

            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"
        SELECT target
        FROM planning.daily_planning_sheet
        WHERE office_id = @office_id
          AND internal_work_order_id = @internal_work_order_id
          AND operation_id = @operation_id
          AND is_active = TRUE
        ORDER BY created_on DESC
        LIMIT 1";  // ✅ Get latest target if multiple exist

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            cmd.Parameters.AddWithValue("@internal_work_order_id", internalWorkOrderId);
            cmd.Parameters.AddWithValue("@operation_id", operationId);

            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
                target = Convert.ToInt32(result);

            if (target == null)
                return NotFound(new { message = $"No target found for officeId={officeId}, internalWorkOrderId={internalWorkOrderId}, operationId={operationId}" });

            return Ok(new { Target = target });
        }

    }
}
