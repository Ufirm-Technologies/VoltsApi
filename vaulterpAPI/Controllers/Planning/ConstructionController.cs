using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Reflection.Emit;
using vaulterpAPI.Models.Planning;

namespace vaulterpAPI.Controllers.Planning
{
    [ApiController]
    [Route("api/planning/[controller]")]
    public class ContructionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public ContructionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ GET by officeId
        [HttpGet]
        public async Task<IActionResult> GetAllByOffice([FromQuery] int officeId)
        {
            var list = new List<ContructionDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = "SELECT * FROM planning.contruction WHERE office_id = @office_id AND is_active = TRUE";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ContructionDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    InternalWoid = reader.IsDBNull(reader.GetOrdinal("internal_woid")) ? null : reader.GetInt32(reader.GetOrdinal("internal_woid")),
                    OperationId = reader.IsDBNull(reader.GetOrdinal("operation_id")) ? null : reader.GetInt32(reader.GetOrdinal("operation_id")),
                    ProductId = reader.IsDBNull(reader.GetOrdinal("product_id")) ? null : reader.GetInt32(reader.GetOrdinal("product_id")),
                    ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? null : reader.GetInt32(reader.GetOrdinal("item_id")),
                    Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? null : reader.GetString(reader.GetOrdinal("specification")),
                    gradecode = reader.IsDBNull(reader.GetOrdinal("grade_code")) ? null : reader.GetString(reader.GetOrdinal("grade_code")),
                    Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString(reader.GetOrdinal("value")),
                    OfficeId = reader.IsDBNull(reader.GetOrdinal("office_id")) ? null : reader.GetInt32(reader.GetOrdinal("office_id")),
                    IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? null : reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on"))
                });
            }

            var grouped = list
    .GroupBy(x => new { x.InternalWoid, x.OperationId, x.ItemId, x.gradecode })
    .Select(g => new
    {
        internalWoid = g.Key.InternalWoid,
        operationId = g.Key.OperationId,
        itemId = g.Key.ItemId,
        ProductId = g.FirstOrDefault()?.ProductId,
        gradecode = g.Key.gradecode,
        items = g.Select(i => new
        {
            i.Id,
            i.Specification,
            i.Value,
            i.CreatedOn,
            i.CreatedBy,
            i.UpdatedBy,
            i.UpdatedOn
        }).ToList()
    })
    .ToList();

            return Ok(grouped);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<ContructionDto> dtoList)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                string? gradeCode = dtoList.FirstOrDefault()?.gradecode;

                if (string.IsNullOrEmpty(gradeCode))
                {
                    // build gradeCode automatically (old logic)
                    var thicknessRow = dtoList.FirstOrDefault(x => x.Specification?.ToLower() == "min. thickness");
                    var colorRow = dtoList.FirstOrDefault(x => x.Specification?.ToLower() == "color");

                    if (thicknessRow != null && thicknessRow.Value != null && thicknessRow.ItemId != null && thicknessRow.OperationId != null)
                    {
                        // Get item name
                        string itemName = "";
                        using (var itemCmd = new NpgsqlCommand("SELECT name FROM inventory.item WHERE id = @item_id", conn, tx))
                        {
                            itemCmd.Parameters.AddWithValue("@item_id", thicknessRow.ItemId);
                            itemName = Convert.ToString(await itemCmd.ExecuteScalarAsync()) ?? "";
                        }

                        // Get operation code
                        string operationCode = "";
                        using (var opCmd = new NpgsqlCommand("SELECT operation_code FROM master.operation_master WHERE operation_id = @operation_id", conn, tx))
                        {
                            opCmd.Parameters.AddWithValue("@operation_id", thicknessRow.OperationId);
                            operationCode = Convert.ToString(await opCmd.ExecuteScalarAsync()) ?? "";
                        }
                        // Get colorVal from colorRow
                        string colorVal = "";
                        if (!string.IsNullOrWhiteSpace(colorRow?.Value))
                        {
                            var val = colorRow.Value.Trim().ToUpper();
                            if (val.Length >= 3)
                            {
                                // First two letters + last letter
                                colorVal = val.Substring(0, 2) + val[^1];
                            }
                            else
                            {
                                // If less than 3 chars, just take full value
                                colorVal = val;
                            }
                        }


                        var prefix = itemName.Length >= 3 ? itemName.Substring(0, 3).ToUpper() : itemName.ToUpper();
                  

                        // GradeCode without thickness
                        gradeCode = prefix + colorVal + operationCode;

                    }
                }

                foreach (var dto in dtoList)
                {
                    // Check if the record already exists
                    var existsQuery = @"
        SELECT COUNT(*) 
        FROM planning.contruction
        WHERE grade_code = @grade_code
          AND specification = @specification
          AND value = @value
          AND internal_woid = @internal_woid
          AND operation_id = @operation_id
          AND product_id = @product_id
          AND item_id = @item_id
          AND office_id = @office_id
          AND is_active = TRUE";

                    using (var existsCmd = new NpgsqlCommand(existsQuery, conn, tx))
                    {
                        existsCmd.Parameters.AddWithValue("@grade_code", (object?)gradeCode ?? DBNull.Value);
                        existsCmd.Parameters.AddWithValue("@specification", (object?)dto.Specification ?? DBNull.Value);
                        existsCmd.Parameters.AddWithValue("@value", (object?)dto.Value ?? DBNull.Value);
                        existsCmd.Parameters.AddWithValue("@internal_woid", (object?)dto.InternalWoid ?? DBNull.Value);
                        existsCmd.Parameters.AddWithValue("@operation_id", (object?)dto.OperationId ?? DBNull.Value);
                        existsCmd.Parameters.AddWithValue("@product_id", (object?)dto.ProductId ?? DBNull.Value);
                        existsCmd.Parameters.AddWithValue("@item_id", (object?)dto.ItemId ?? DBNull.Value);
                        existsCmd.Parameters.AddWithValue("@office_id", (object?)dto.OfficeId ?? DBNull.Value);

                        var exists = (long)(await existsCmd.ExecuteScalarAsync() ?? 0);

                        if (exists > 0)
                        {
                            // Skip insert if already exists
                            continue;
                        }
                    }

                    // Insert only if not exists
                    var insertQuery = @"
        INSERT INTO planning.contruction 
        (internal_woid, operation_id, product_id, item_id, specification, value, grade_code, office_id, is_active, created_by, created_on)
        VALUES
        (@internal_woid, @operation_id, @product_id, @item_id, @specification, @value, @grade_code, @office_id, TRUE, @created_by, NOW())";

                    using var insertCmd = new NpgsqlCommand(insertQuery, conn, tx);
                    insertCmd.Parameters.AddWithValue("@internal_woid", (object?)dto.InternalWoid ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@operation_id", (object?)dto.OperationId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@product_id", (object?)dto.ProductId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@item_id", (object?)dto.ItemId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@specification", (object?)dto.Specification ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@value", (object?)dto.Value ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@grade_code", (object?)gradeCode ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@office_id", (object?)dto.OfficeId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@created_by", (object?)dto.CreatedBy ?? DBNull.Value);

                    await insertCmd.ExecuteNonQueryAsync();
                }


                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return await GetAllByOffice(dtoList.First().OfficeId ?? 0);
        }

        [HttpPut("{internalWoid}")]
        public async Task<IActionResult> Update(int internalWoid, [FromBody] List<ContructionDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest("No data provided");

            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // --- Build grade code once (same as in POST) ---
                string? gradeCode = null;
                var thicknessRow = dtos.FirstOrDefault(x => x.Specification?.ToLower() == "min. thickness");
                var colorRow = dtos.FirstOrDefault(x => x.Specification?.ToLower() == "color");

                if (thicknessRow != null && !string.IsNullOrWhiteSpace(thicknessRow.Value) &&
                    thicknessRow.ItemId != null && thicknessRow.OperationId != null)
                {
                    string itemName;
                    await using (var itemCmd = new NpgsqlCommand(
                        "SELECT name FROM inventory.item WHERE id = @item_id", conn, tx))
                    {
                        itemCmd.Parameters.AddWithValue("@item_id", thicknessRow.ItemId);
                        itemName = Convert.ToString(await itemCmd.ExecuteScalarAsync()) ?? "";
                    }

                    string operationCode;
                    await using (var opCmd = new NpgsqlCommand(
                        "SELECT operation_code FROM master.operation_master WHERE operation_id = @operation_id", conn, tx))
                    {
                        opCmd.Parameters.AddWithValue("@operation_id", thicknessRow.OperationId);
                        operationCode = Convert.ToString(await opCmd.ExecuteScalarAsync()) ?? "";
                    }

                    var prefix = itemName.Length >= 3
                        ? itemName.Substring(0, 3).ToUpper()
                        : itemName.ToUpper();

                    // Get colorVal from colorRow
                    string colorVal = "";
                    if (!string.IsNullOrWhiteSpace(colorRow?.Value))
                    {
                        var val = colorRow.Value.Trim().ToUpper();
                        if (val.Length >= 3)
                        {
                            // First two letters + last letter
                            colorVal = val.Substring(0, 2) + val[^1];
                        }
                        else
                        {
                            // If less than 3 chars, just take full value
                            colorVal = val;
                        }
                    }


                    var thicknessVal = thicknessRow.Value.PadLeft(3, '0');
                

                    // GradeCode without thickness
                    gradeCode = prefix + colorVal + operationCode;
                }

                // --- STEP 1: Fetch all existing rows for this internalWoid ---
                var existingRows = new List<(int Id, int? ItemId, int? OperationId, string? Specification, string? GradeCode)>();
                const string selectQuery = @"
            SELECT id, item_id, operation_id, specification, grade_code
            FROM planning.contruction
            WHERE internal_woid = @internal_woid AND is_active = TRUE";
                await using (var selectCmd = new NpgsqlCommand(selectQuery, conn, tx))
                {
                    selectCmd.Parameters.AddWithValue("@internal_woid", internalWoid);
                    await using var reader = await selectCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        existingRows.Add((
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetInt32(1),
                            reader.IsDBNull(2) ? null : reader.GetInt32(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.IsDBNull(4) ? null : reader.GetString(4)
                        ));
                    }
                }

                // --- STEP 2: Process incoming dtos (insert/update/delete) ---
                var keptIds = new HashSet<int>();

                foreach (var dto in dtos)
                {
                    // if value is empty → delete
                    if (string.IsNullOrWhiteSpace(dto.Value) && dto.Id > 0)
                    {
                        const string deleteQuery = @"DELETE FROM planning.contruction WHERE id = @id";
                        await using var deleteCmd = new NpgsqlCommand(deleteQuery, conn, tx);
                        deleteCmd.Parameters.AddWithValue("@id", dto.Id);
                        await deleteCmd.ExecuteNonQueryAsync();
                        continue;
                    }

                    if (dto.Id > 0 && !string.IsNullOrWhiteSpace(dto.Value))
                    {
                        // update existing row
                        const string updateQuery = @"
                    UPDATE planning.contruction
                    SET value = @value,
                        grade_code = @grade_code,
                        office_id = @office_id,
                        updated_by = @updated_by,
                        updated_on = NOW(),
                        is_active = TRUE
                    WHERE id = @id";

                        await using var updateCmd = new NpgsqlCommand(updateQuery, conn, tx);
                        updateCmd.Parameters.AddWithValue("@id", dto.Id);
                        updateCmd.Parameters.AddWithValue("@value", dto.Value);
                        updateCmd.Parameters.AddWithValue("@grade_code",
                            (dto.Specification?.ToLower() == "min. thickness" || dto.Specification?.ToLower() == "color")
                                ? (object?)gradeCode ?? DBNull.Value
                                : DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@office_id", (object?)dto.OfficeId ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@updated_by", (object?)dto.UpdatedBy ?? DBNull.Value);

                        await updateCmd.ExecuteNonQueryAsync();
                        keptIds.Add(dto.Id);
                    }
                    else if (!string.IsNullOrWhiteSpace(dto.Value))
                    {
                        // insert new row
                        const string insertQuery = @"
                    INSERT INTO planning.contruction
                        (internal_woid, operation_id, product_id, item_id,
                         specification, value, grade_code, office_id,
                         created_by, created_on, updated_by, updated_on, is_active)
                    VALUES
                        (@internal_woid, @operation_id, @product_id, @item_id,
                         @specification, @value, @grade_code, @office_id,
                         @created_by, NOW(), @updated_by, NOW(), TRUE)
                    RETURNING id";

                        await using var insertCmd = new NpgsqlCommand(insertQuery, conn, tx);
                        insertCmd.Parameters.AddWithValue("@internal_woid", dto.InternalWoid ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@operation_id", dto.OperationId ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@product_id", dto.ProductId ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@item_id", dto.ItemId ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@specification", dto.Specification ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@value", dto.Value);
                        insertCmd.Parameters.AddWithValue("@grade_code",
                            (dto.Specification?.ToLower() == "min. thickness" || dto.Specification?.ToLower() == "color")
                                ? (object?)gradeCode ?? DBNull.Value
                                : DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@office_id", dto.OfficeId ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@created_by", dto.CreatedBy ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@updated_by", dto.UpdatedBy ?? (object)DBNull.Value);

                        var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                        keptIds.Add(newId);
                    }
                }

                // --- STEP 3: Mark missing rows inactive ---
                foreach (var row in existingRows)
                {
                    if (!keptIds.Contains(row.Id))
                    {
                        const string inactiveQuery = @"
                    UPDATE planning.contruction
                    SET is_active = FALSE, updated_on = NOW()
                    WHERE id = @id";
                        await using var inactiveCmd = new NpgsqlCommand(inactiveQuery, conn, tx);
                        inactiveCmd.Parameters.AddWithValue("@id", row.Id);
                        await inactiveCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return Ok("Specifications updated successfully");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error updating specifications: {ex.Message}");
            }
        }

        // ✅ DELETE by internalWoid (soft delete)
        [HttpDelete]
        public async Task<IActionResult> DeleteByInternalWoid([FromQuery] int internalWoid)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var deleteQuery = @"
                UPDATE planning.contruction
                SET is_active = FALSE
                WHERE internal_woid = @internal_woid AND is_active = TRUE";

            using var deleteCmd = new NpgsqlCommand(deleteQuery, conn);
            deleteCmd.Parameters.AddWithValue("@internal_woid", internalWoid);

            var rows = await deleteCmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = $"No active rows found for internal_woid = {internalWoid}" });

            return Ok(new { message = $"{rows} rows deleted with internal_woid = {internalWoid}" });
        }

        // GET itemIds by internalWoid
        [HttpGet("items-by-woid")]
        public async Task<IActionResult> GetItemIdsByInternalWoid([FromQuery] int internalWoid)
        {
            var itemIds = new List<int>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"
        SELECT DISTINCT item_id
        FROM planning.contruction
        WHERE internal_woid = @internal_woid 
          AND is_active = TRUE
          AND item_id IS NOT NULL";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@internal_woid", internalWoid);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                itemIds.Add(reader.GetInt32(reader.GetOrdinal("item_id")));
            }

            if (!itemIds.Any())
            {
                return NotFound(new { message = $"No active items found for internal_woid = {internalWoid}" });
            }

            return Ok(new { internalWoid, itemIds });
        }
        // ✅ GET all distinct grade codes by officeId
        [HttpGet("grade-codes")]
        public async Task<IActionResult> GetGradeCodes([FromQuery] int officeId)
        {
            var codes = new List<string>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"
        SELECT DISTINCT grade_code
        FROM planning.contruction
        WHERE office_id = @office_id 
          AND is_active = TRUE
          AND grade_code IS NOT NULL";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                codes.Add(reader.GetString(0));
            }

            return Ok(codes);
        }
        // ✅ GET all rows for a given grade code
        [HttpGet("by-gradecode")]
        public async Task<IActionResult> GetByGradeCode([FromQuery] string gradeCode, [FromQuery] int officeId)
        {
            var list = new List<ContructionDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"
        SELECT * 
        FROM planning.contruction
        WHERE grade_code = @grade_code
          AND office_id = @office_id
          AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@grade_code", gradeCode);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ContructionDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    InternalWoid = reader.IsDBNull(reader.GetOrdinal("internal_woid")) ? null : reader.GetInt32(reader.GetOrdinal("internal_woid")),
                    OperationId = reader.IsDBNull(reader.GetOrdinal("operation_id")) ? null : reader.GetInt32(reader.GetOrdinal("operation_id")),
                    ProductId = reader.IsDBNull(reader.GetOrdinal("product_id")) ? null : reader.GetInt32(reader.GetOrdinal("product_id")),
                    ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? null : reader.GetInt32(reader.GetOrdinal("item_id")),
                    Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? null : reader.GetString(reader.GetOrdinal("specification")),
                    gradecode = reader.IsDBNull(reader.GetOrdinal("grade_code")) ? null : reader.GetString(reader.GetOrdinal("grade_code")),
                    Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString(reader.GetOrdinal("value")),
                    OfficeId = reader.IsDBNull(reader.GetOrdinal("office_id")) ? null : reader.GetInt32(reader.GetOrdinal("office_id")),
                    IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? null : reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on"))
                });
            }

            return Ok(list);
        }

        // GET distinct grade codes by internalWoid
        [HttpGet("gradecodes-by-woid")]
        public async Task<IActionResult> GetGradeCodesByInternalWoid([FromQuery] int internalWoid)
        {
            var gradeCodes = new List<string>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"
        SELECT DISTINCT grade_code
        FROM planning.contruction
        WHERE internal_woid = @internal_woid
          AND is_active = TRUE
          AND grade_code IS NOT NULL";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@internal_woid", internalWoid);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                gradeCodes.Add(reader.GetString(0));
            }

            return Ok(gradeCodes);
        }

        [HttpGet("by-item-woid-grouped")]
        public async Task<IActionResult> GetByItemOfficeAndWoidGrouped([FromQuery] int itemId, [FromQuery] int officeId, [FromQuery] int internalWoid)
        {
            var list = new List<ContructionDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"
        SELECT * 
        FROM planning.contruction
        WHERE item_id = @item_id
          AND office_id = @office_id
          AND internal_woid = @internal_woid
          AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@item_id", itemId);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            cmd.Parameters.AddWithValue("@internal_woid", internalWoid);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ContructionDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    InternalWoid = reader.IsDBNull(reader.GetOrdinal("internal_woid")) ? null : reader.GetInt32(reader.GetOrdinal("internal_woid")),
                    OperationId = reader.IsDBNull(reader.GetOrdinal("operation_id")) ? null : reader.GetInt32(reader.GetOrdinal("operation_id")),
                    ProductId = reader.IsDBNull(reader.GetOrdinal("product_id")) ? null : reader.GetInt32(reader.GetOrdinal("product_id")),
                    ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? null : reader.GetInt32(reader.GetOrdinal("item_id")),
                    Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? null : reader.GetString(reader.GetOrdinal("specification")),
                    gradecode = reader.IsDBNull(reader.GetOrdinal("grade_code")) ? null : reader.GetString(reader.GetOrdinal("grade_code")),
                    Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString(reader.GetOrdinal("value")),
                    OfficeId = reader.IsDBNull(reader.GetOrdinal("office_id")) ? null : reader.GetInt32(reader.GetOrdinal("office_id")),
                    IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? null : reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on"))
                });
            }

            var grouped = list
                .GroupBy(x => new { x.ItemId, x.gradecode, x.OperationId, x.InternalWoid })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    GradeCode = g.Key.gradecode,
                    ProductId = g.FirstOrDefault()?.ProductId,
                    OperationId = g.Key.OperationId,
                    InternalWoid = g.Key.InternalWoid,
                    Specifications = g.Select(i => new
                    {
                        i.Id,
                        i.Specification,
                        i.Value,
                        i.CreatedOn,
                        i.CreatedBy,
                        i.UpdatedBy,
                        i.UpdatedOn
                    }).ToList()
                })
                .ToList();

            return Ok(grouped);
        }

        [HttpGet("by-operation-woid-grouped")]
        public async Task<IActionResult> GetByoperationOfficeAndWoid([FromQuery] int operationId, [FromQuery] int officeId, [FromQuery] int internalWoid)
        {
            var list = new List<ContructionDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"
        SELECT * 
        FROM planning.contruction
        WHERE operation_id = @operation_id
          AND office_id = @office_id
          AND internal_woid = @internal_woid
          AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@operation_id", operationId);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            cmd.Parameters.AddWithValue("@internal_woid", internalWoid);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ContructionDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    InternalWoid = reader.IsDBNull(reader.GetOrdinal("internal_woid")) ? null : reader.GetInt32(reader.GetOrdinal("internal_woid")),
                    OperationId = reader.IsDBNull(reader.GetOrdinal("operation_id")) ? null : reader.GetInt32(reader.GetOrdinal("operation_id")),
                    ProductId = reader.IsDBNull(reader.GetOrdinal("product_id")) ? null : reader.GetInt32(reader.GetOrdinal("product_id")),
                    ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? null : reader.GetInt32(reader.GetOrdinal("item_id")),
                    Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? null : reader.GetString(reader.GetOrdinal("specification")),
                    gradecode = reader.IsDBNull(reader.GetOrdinal("grade_code")) ? null : reader.GetString(reader.GetOrdinal("grade_code")),
                    Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString(reader.GetOrdinal("value")),
                    OfficeId = reader.IsDBNull(reader.GetOrdinal("office_id")) ? null : reader.GetInt32(reader.GetOrdinal("office_id")),
                    IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? null : reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updated_by")) ? null : reader.GetInt32(reader.GetOrdinal("updated_by")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on"))
                });
            }

            var grouped = list
                .GroupBy(x => new { x.ItemId, x.gradecode, x.OperationId, x.InternalWoid })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    GradeCode = g.Key.gradecode,
                    ProductId = g.FirstOrDefault()?.ProductId,
                    OperationId = g.Key.OperationId,
                    InternalWoid = g.Key.InternalWoid,
                    Specifications = g.Select(i => new
                    {
                        i.Id,
                        i.Specification,
                        i.Value,
                        i.CreatedOn,
                        i.CreatedBy,
                        i.UpdatedBy,
                        i.UpdatedOn
                    }).ToList()
                })
                .ToList();

            return Ok(grouped);
        }
    }
}
