using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Process;

namespace vaulterpAPI.Controllers.Process
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ProcessController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // CREATE
        [HttpPost("create")]
        public async Task<IActionResult> CreateProcessWithOperations([FromBody] ProcessWithOperationsDto data)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                const string insertProcess = @"
                    INSERT INTO master.process_master
                    (process_name, office_id, is_active, created_by, created_on)
                    VALUES (@name, @office, true, @created_by, CURRENT_TIMESTAMP)
                    RETURNING process_id";

                var processCmd = new NpgsqlCommand(insertProcess, conn, transaction);
                processCmd.Parameters.AddWithValue("@name", data.ProcessName);
                processCmd.Parameters.AddWithValue("@office", data.OfficeId);
                processCmd.Parameters.AddWithValue("@created_by", data.CreatedBy);
                var processId = (int)await processCmd.ExecuteScalarAsync();

                foreach (var op in data.Operations)
                {
                    const string insertOp = @"
                        INSERT INTO planning.process_operations
                        (process_id, operation_id, step_order, office_id, is_active, created_by, created_on)
                        VALUES (@pid, @opid, @step, @office, true, @created_by, CURRENT_TIMESTAMP)";

                    var opCmd = new NpgsqlCommand(insertOp, conn, transaction);
                    opCmd.Parameters.AddWithValue("@pid", processId);
                    opCmd.Parameters.AddWithValue("@opid", op.OperationId);
                    opCmd.Parameters.AddWithValue("@step", op.StepOrder);
                    opCmd.Parameters.AddWithValue("@office", data.OfficeId);
                    opCmd.Parameters.AddWithValue("@created_by", data.CreatedBy);
                    await opCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { Message = "Process and operations created successfully", ProcessId = processId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET BY ID
        [HttpGet("{processId}")]
        public async Task<IActionResult> GetProcessById(int processId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string query = @"
                SELECT 
                    pm.process_id,
                    pm.process_name,
                    pm.office_id,
                    COALESCE(pm.created_by, 0),
                    COALESCE(pm.updated_by, 0),
                    po.operation_id,
                    po.step_order
                FROM master.process_master pm
                LEFT JOIN planning.process_operations po 
                    ON pm.process_id = po.process_id AND po.is_active = true
                WHERE pm.process_id = @processId AND pm.is_active = true";

            var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@processId", processId);

            ProcessWithOperationsDto result = null;
            var operations = new List<OperationDto>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (result == null)
                {
                    result = new ProcessWithOperationsDto
                    {
                        ProcessId = reader.GetInt32(0),
                        ProcessName = reader.GetString(1),
                        OfficeId = reader.GetInt32(2),
                        CreatedBy = reader.GetInt32(3),
                        UpdatedBy = reader.GetInt32(4),
                        Operations = operations
                    };
                }

                if (!reader.IsDBNull(5))
                {
                    operations.Add(new OperationDto
                    {
                        OperationId = reader.GetInt32(5),
                        StepOrder = reader.GetInt32(6)
                    });
                }
            }

            if (result == null)
                return NotFound(new { message = "No process found" });

            return Ok(result);
        }

        // GET ALL BY OFFICE
        [HttpGet("office/{officeId}")]
        public async Task<IActionResult> GetProcessesByOfficeId(int officeId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string query = @"
                SELECT 
                    pm.process_id,
                    pm.process_name,
                    pm.office_id,
                    COALESCE(pm.created_by, 0),
                    COALESCE(pm.updated_by, 0),
                    po.operation_id,
                    po.step_order
                FROM master.process_master pm
                LEFT JOIN planning.process_operations po 
                    ON pm.process_id = po.process_id AND po.is_active = true
                WHERE pm.office_id = @officeId AND pm.is_active = true
                ORDER BY pm.process_id, po.step_order";

            var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@officeId", officeId);

            var processes = new Dictionary<int, ProcessWithOperationsDto>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var processId = reader.GetInt32(0);
                if (!processes.TryGetValue(processId, out var dto))
                {
                    dto = new ProcessWithOperationsDto
                    {
                        ProcessId = processId,
                        ProcessName = reader.GetString(1),
                        OfficeId = reader.GetInt32(2),
                        CreatedBy = reader.GetInt32(3),
                        UpdatedBy = reader.GetInt32(4),
                        Operations = new List<OperationDto>()
                    };
                    processes[processId] = dto;
                }

                if (!reader.IsDBNull(5))
                {
                    dto.Operations.Add(new OperationDto
                    {
                        OperationId = reader.GetInt32(5),
                        StepOrder = reader.GetInt32(6)
                    });
                }
            }

            return Ok(processes.Values);
        }

        // DELETE (SOFT)
        [HttpDelete("{processId}")]
        public async Task<IActionResult> SoftDeleteProcess(int processId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                const string deleteProcess = @"
                    UPDATE master.process_master
                    SET is_active = false,
                        updated_by = @updated_by,
                        updated_on = CURRENT_TIMESTAMP
                    WHERE process_id = @processId";

                var processCmd = new NpgsqlCommand(deleteProcess, conn, transaction);
                processCmd.Parameters.AddWithValue("@processId", processId);
                processCmd.Parameters.AddWithValue("@updated_by", 0); // Replace with user ID if available
                await processCmd.ExecuteNonQueryAsync();

                const string deleteOps = @"
                    UPDATE planning.process_operations
                    SET is_active = false,
                        updated_by = @updated_by,
                        updated_on = CURRENT_TIMESTAMP
                    WHERE process_id = @processId";

                var opCmd = new NpgsqlCommand(deleteOps, conn, transaction);
                opCmd.Parameters.AddWithValue("@processId", processId);
                opCmd.Parameters.AddWithValue("@updated_by", 0); // Replace with user ID if available
                await opCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return Ok(new { Message = "Process and its operations deleted (soft) successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // UPDATE
        [HttpPut("{processId}")]
        public async Task<IActionResult> UpdateProcessOperations(int processId, [FromBody] ProcessWithOperationsDto data)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                const string deactivateOldOps = @"
                    UPDATE planning.process_operations
                    SET is_active = false,
                        updated_by = @updated_by,
                        updated_on = CURRENT_TIMESTAMP
                    WHERE process_id = @processId";

                var deactivateCmd = new NpgsqlCommand(deactivateOldOps, conn, transaction);
                deactivateCmd.Parameters.AddWithValue("@processId", processId);
                deactivateCmd.Parameters.AddWithValue("@updated_by", data.UpdatedBy);
                await deactivateCmd.ExecuteNonQueryAsync();

                foreach (var op in data.Operations)
                {
                    const string insertOp = @"
                        INSERT INTO planning.process_operations
                        (process_id, operation_id, step_order, office_id, is_active, created_by, created_on)
                        VALUES (@pid, @opid, @step, @office, true, @created_by, CURRENT_TIMESTAMP)";

                    var insertCmd = new NpgsqlCommand(insertOp, conn, transaction);
                    insertCmd.Parameters.AddWithValue("@pid", processId);
                    insertCmd.Parameters.AddWithValue("@opid", op.OperationId);
                    insertCmd.Parameters.AddWithValue("@step", op.StepOrder);
                    insertCmd.Parameters.AddWithValue("@office", data.OfficeId);
                    insertCmd.Parameters.AddWithValue("@created_by", data.CreatedBy);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { Message = "Process operations updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
