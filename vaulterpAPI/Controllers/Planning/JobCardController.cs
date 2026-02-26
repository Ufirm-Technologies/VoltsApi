using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Planning;

namespace vaulterpAPI.Controllers.Planning
{
    [ApiController]
    [Route("api/planning/[controller]")]
    public class JobCardController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public JobCardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ GET all by officeId
        [HttpGet]
        public async Task<IActionResult> GetAllByOffice([FromQuery] int officeId)
        {
            var list = new List<JobCardDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = "SELECT * FROM planning.job_card WHERE office_id = @office_id AND is_active = TRUE";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new JobCardDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    InternalWo = reader.IsDBNull(reader.GetOrdinal("internal_wo")) ? null : reader.GetInt32(reader.GetOrdinal("internal_wo")),
                    Date = reader.IsDBNull(reader.GetOrdinal("date")) ? null : reader.GetDateTime(reader.GetOrdinal("date")),
                    ShiftId = reader.IsDBNull(reader.GetOrdinal("shift_id")) ? null : reader.GetInt32(reader.GetOrdinal("shift_id")),
                    AssetId = reader.IsDBNull(reader.GetOrdinal("asset_id")) ? null : reader.GetInt32(reader.GetOrdinal("asset_id")),
                    ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? null : reader.GetInt32(reader.GetOrdinal("item_id")),
                    Compected = reader.IsDBNull(reader.GetOrdinal("compected")) ? null : reader.GetInt32(reader.GetOrdinal("compected")),
                    NoDiaOfAmWire = reader.IsDBNull(reader.GetOrdinal("no_dia_of_am_wire")) ? null : reader.GetString(reader.GetOrdinal("no_dia_of_am_wire")),
                    PayOffDNo = reader.IsDBNull(reader.GetOrdinal("pay_off_d_no")) ? null : reader.GetString(reader.GetOrdinal("pay_off_d_no")),
                    TakeUpDrumSize = reader.IsDBNull(reader.GetOrdinal("take_up_drum_size")) ? null : reader.GetString(reader.GetOrdinal("take_up_drum_size")),
                    Embrossing = reader.IsDBNull(reader.GetOrdinal("embrossing")) ? null : reader.GetString(reader.GetOrdinal("embrossing")),
                    Remark = reader.IsDBNull(reader.GetOrdinal("remark")) ? null : reader.GetString(reader.GetOrdinal("remark")),
                    IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? null : reader.GetBoolean(reader.GetOrdinal("is_active")),
                    OfficeId = reader.IsDBNull(reader.GetOrdinal("office_id")) ? null : reader.GetInt32(reader.GetOrdinal("office_id")),
                    GradeCode= reader.IsDBNull(reader.GetOrdinal("grade_code")) ? null : reader.GetString(reader.GetOrdinal("grade_code")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on")),
                    OperationId = reader.IsDBNull(reader.GetOrdinal("operation_id")) ? null : reader.GetInt32(reader.GetOrdinal("operation_id")),
                    OperatorId = reader.IsDBNull(reader.GetOrdinal("operator_id")) ? null : reader.GetInt32(reader.GetOrdinal("operator_id")),
                });
            }

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JobCardDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                var query = @"
            INSERT INTO planning.job_card
            (internal_wo, date, shift_id, asset_id, item_id, compected, no_dia_of_am_wire,
             pay_off_d_no, take_up_drum_size, embrossing, remark, is_active, office_id, grade_code, created_by,
             created_on, updated_by, updated_on, operation_id,operator_id)
            VALUES
            (@internal_wo,  @date, @shift_id, @asset_id, @item_id, @compected, @no_dia_of_am_wire,
             @pay_off_d_no, @take_up_drum_size, @embrossing, @remark, TRUE, @office_id, @grade_code, @created_by,
             NOW(), @updated_by, NOW(), @operation_id,@operator_id)";

                using var cmd = new NpgsqlCommand(query, conn, tx);
                cmd.Parameters.AddWithValue("@internal_wo", (object?)dto.InternalWo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@date", (object?)dto.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@shift_id", (object?)dto.ShiftId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@asset_id", (object?)dto.AssetId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@item_id", (object?)dto.ItemId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@compected", (object?)dto.Compected ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@no_dia_of_am_wire", (object?)dto.NoDiaOfAmWire ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@pay_off_d_no", (object?)dto.PayOffDNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@take_up_drum_size", (object?)dto.TakeUpDrumSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@embrossing", (object?)dto.Embrossing ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@remark", (object?)dto.Remark ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@office_id", (object?)dto.OfficeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@grade_code", (object?)dto.GradeCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_by", (object?)dto.CreatedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@operation_id", (object?)dto.OperationId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@operator_id", (object?)dto.OperatorId ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return await GetAllByOffice(dto.OfficeId ?? 0);
        }


        // ✅ PUT by Id (update single row)
        [HttpPut("by-id/{id}")]
        public async Task<IActionResult> UpdateById(int id, [FromBody] JobCardDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid data" });

            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"
        UPDATE planning.job_card
        SET internal_wo = @internal_wo,
            date = @date,
            shift_id = @shift_id,
            asset_id = @asset_id,
            item_id = @item_id,
            compected = @compected,
            no_dia_of_am_wire = @no_dia_of_am_wire,
            pay_off_d_no = @pay_off_d_no,
            take_up_drum_size = @take_up_drum_size,
            embrossing = @embrossing,
            remark = @remark,
            office_id = @office_id,
            grade_code = @grade_code,
            updated_by = @updated_by,
            updated_on = NOW(),
            operation_id = @operation_id,
            operator_id = @operator_id
        WHERE id = @id AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@internal_wo", (object?)dto.InternalWo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@date", (object?)dto.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@shift_id", (object?)dto.ShiftId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@asset_id", (object?)dto.AssetId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@item_id", (object?)dto.ItemId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@compected", (object?)dto.Compected ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@no_dia_of_am_wire", (object?)dto.NoDiaOfAmWire ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pay_off_d_no", (object?)dto.PayOffDNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@take_up_drum_size", (object?)dto.TakeUpDrumSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@embrossing", (object?)dto.Embrossing ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@remark", (object?)dto.Remark ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@office_id", (object?)dto.OfficeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@grade_code", (object?)dto.GradeCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@operation_id", (object?)dto.OperationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@operator_id", (object?)dto.OperatorId ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = $"No active JobCard found with id = {id}" });

            return Ok(new { message = $"JobCard with id {id} updated successfully" });
        }

        // ✅ DELETE by internal_wo (soft delete)
        [HttpDelete("by-id/{id}")]
        public async Task<IActionResult> DeleteById(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"
        UPDATE planning.job_card
        SET is_active = FALSE, updated_on = NOW()
        WHERE id = @id AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = $"No active JobCard found with id = {id}" });

            return Ok(new { message = $"JobCard with id {id} deleted successfully" });
        }

        // ✅ GET all JobCard IDs by internal work order
        [HttpGet("by-internal-wo/{internalWo}")]
        public async Task<IActionResult> GetIdsByInternalWo(int internalWo)
        {
            var ids = new List<int>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = "SELECT id FROM planning.job_card WHERE internal_wo = @internal_wo AND is_active = TRUE";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@internal_wo", internalWo);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(reader.GetOrdinal("id")));
            }

            if (ids.Count == 0)
                return NotFound(new { message = $"No active JobCards found for internal_wo = {internalWo}" });

            return Ok(ids);
        }

        // ✅ GET all Operations linked to a JobCard
        [HttpGet("operations/by-jobcard/{jobCardId}")]
        public async Task<IActionResult> GetOperationsByJobCard(int jobCardId)
        {
            var operations = new List<object>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // 🔍 This assumes you have a table `planning.operation` with at least id & name
            var query = @"
        SELECT o.operation_id, o.operation_name, o.description
        FROM master.operation_master o
        INNER JOIN planning.job_card jc ON jc.operation_id = o.operation_id
        WHERE jc.id = @jobCardId AND jc.is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@jobCardId", jobCardId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                operations.Add(new
                {
                    Id = reader.GetInt32(reader.GetOrdinal("operation_id")),
                    Name = reader.IsDBNull(reader.GetOrdinal("operation_name")) ? null : reader.GetString(reader.GetOrdinal("operation_name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description"))
                });
            }

            if (operations.Count == 0)
                return NotFound(new { message = $"No operations found for JobCard ID = {jobCardId}" });

            return Ok(operations);
        }

        // ✅ GET TakeUpDrumSize by OfficeId & InternalWo
        // ✅ GET TakeUpDrumSize & OperationId by OfficeId & InternalWo
        [HttpGet("take-up-drum-size-and-operation")]
        public async Task<IActionResult> GetTakeUpDrumSizeAndOperation(
            [FromQuery] int officeId,
            [FromQuery] int internalWo)
        {
            var results = new List<object>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var query = @"
        SELECT DISTINCT take_up_drum_size, operation_id
        FROM planning.job_card
        WHERE office_id = @office_id 
          AND internal_wo = @internal_wo
          AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            cmd.Parameters.AddWithValue("@internal_wo", internalWo);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    TakeUpDrumSize = reader.IsDBNull(reader.GetOrdinal("take_up_drum_size"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("take_up_drum_size")),
                    OperationId = reader.IsDBNull(reader.GetOrdinal("operation_id"))
                });
            }

            if (results.Count == 0)
                return NotFound(new { message = $"No records found for officeId = {officeId}, internalWo = {internalWo}" });

            return Ok(results);
        }

    }
}
