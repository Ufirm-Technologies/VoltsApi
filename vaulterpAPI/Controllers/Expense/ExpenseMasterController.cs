using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Expense;
using System.Data;

namespace vaulterpAPI.Controllers.Expense
{
    [ApiController]
    [Route("api/master/[controller]")]
    public class ExpenseMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ExpenseMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ GET: All expenses grouped by type with subtypes
        [HttpGet("by-office/{officeId}")]
        public async Task<IActionResult> GetByOffice(int officeId)
        {
            var expenses = new List<ExpenseGroupDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"
                SELECT expense_type,
                       array_agg(DISTINCT expense_subtype ORDER BY expense_subtype) AS subtypes,
                       office_id,
                       MIN(created_at) AS created_on,
                       MIN(created_by) AS created_by,
                       BOOL_AND(is_active) AS is_active
                FROM master.expense_master
                WHERE office_id = @office_id AND is_active = TRUE
                GROUP BY expense_type, office_id
                ORDER BY expense_type";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                expenses.Add(new ExpenseGroupDto
                {
                    Type = reader.GetString(0),
                    SubTypes = reader.IsDBNull(1) ? new List<string>() : reader.GetFieldValue<string[]>(1).ToList(),
                    OfficeId = reader.GetInt32(2),
                    CreatedOn = reader.GetDateTime(3),
                    CreatedBy = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5)
                });
            }

            return Ok(expenses);
        }

        // ✅ GET: Subtypes by type
        [HttpGet("subtypes/{officeId}/{expenseType}")]
        public async Task<IActionResult> GetSubTypesByType(int officeId, string expenseType)
        {
            var subtypes = new List<string>();
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"SELECT DISTINCT expense_subtype 
                          FROM master.expense_master 
                          WHERE expense_type = @expense_type 
                            AND office_id = @office_id 
                            AND is_active = TRUE
                          ORDER BY expense_subtype";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@expense_type", expenseType);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                subtypes.Add(reader.GetString(0));
            }

            return Ok(subtypes);
        }

        // ✅ POST: Create Expense (with multiple subtypes)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExpenseGroupDto dto)
        {
            if (dto.SubTypes == null || !dto.SubTypes.Any())
                return BadRequest(new { message = "At least one subtype is required." });

            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            foreach (var subtype in dto.SubTypes)
            {
                var query = @"INSERT INTO master.expense_master 
                              (office_id, expense_type, expense_subtype, created_by, created_at, updated_at, is_active)
                              VALUES (@office_id, @type, @subtype, @created_by, NOW(), NOW(), TRUE)";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);
                cmd.Parameters.AddWithValue("@type", dto.Type);
                cmd.Parameters.AddWithValue("@subtype", subtype);
                cmd.Parameters.AddWithValue("@created_by", (object?)dto.CreatedBy ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }

            return Ok(new { message = "Expense type and subtypes created successfully" });
        }

        [HttpPut("{expenseType}")]
        public async Task<IActionResult> UpdateByType(string expenseType, [FromBody] ExpenseUpdateDto dto)
        {
            if (string.IsNullOrEmpty(expenseType))
                return BadRequest(new { message = "Expense type is required" });

            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // Step 1: Soft delete all existing records of this type
            var deleteQuery = @"UPDATE master.expense_master 
                        SET is_active = FALSE, updated_by = @updated_by, updated_at = NOW()
                        WHERE expense_type = @expense_type AND is_active = TRUE";

            using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
            {
                deleteCmd.Parameters.AddWithValue("@expense_type", expenseType);
                deleteCmd.Parameters.AddWithValue("@updated_by", dto.CreatedBy);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // Step 2: Insert new subtypes for the same type
            foreach (var subType in dto.SubTypes)
            {
                var insertQuery = @"INSERT INTO master.expense_master 
                            (expense_type, expense_subtype, office_id, is_active, created_at, created_by, updated_at, updated_by) 
                            VALUES (@expense_type, @expense_subtype, @office_id, @is_active, @created_at, @created_by, NOW(), @created_by)";

                using var insertCmd = new NpgsqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@expense_type", expenseType);
                insertCmd.Parameters.AddWithValue("@expense_subtype", subType);
                insertCmd.Parameters.AddWithValue("@office_id", dto.OfficeId);
                insertCmd.Parameters.AddWithValue("@is_active", dto.IsActive);
                insertCmd.Parameters.AddWithValue("@created_at", dto.CreatedOn);
                insertCmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);

                await insertCmd.ExecuteNonQueryAsync();
            }

            return Ok(new { message = $"Expense type '{expenseType}' updated successfully." });
        }

        // ✅ DELETE: Soft delete all expenses by type for given office
        [HttpDelete("{expenseType}")]
        public async Task<IActionResult> DeleteByType(string expenseType, [FromQuery] int? updatedBy = null)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE master.expense_master 
                  SET is_active = FALSE, updated_by = @updated_by, updated_at = NOW()
                  WHERE expense_type = @expense_type AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@expense_type", expenseType);
            cmd.Parameters.AddWithValue("@updated_by", (object?)updatedBy ?? DBNull.Value);

            await conn.OpenAsync();
            var affected = await cmd.ExecuteNonQueryAsync();

            return affected > 0
                ? Ok(new { message = $"All expenses of type '{expenseType}' soft deleted." })
                : NotFound(new { message = "No active records found for given type." });
        }

        [HttpGet("{officeId}")]
        public IActionResult GetExpenseReport(int officeId)
        {
            var result = new List<object>();
            string connString = _configuration.GetConnectionString("DefaultConnection");

            string query = @"
                WITH unified AS (

                    SELECT
    id,
    asset_id,
    'Spare Part Service' AS expense_type,
    spare_id::text AS expense_subtype, -- assuming spare_id is the identifier for the spare part
    issue_date::date AS date_from,
    COALESCE(actual_return_date::date, expected_return_date::date) AS date_to,
    net_cost AS amount,
    'Spare Part Service' AS source
FROM asset.asset_spare_maintenance

                    UNION ALL

                    SELECT 
                        id,
                        assetid AS asset_id,
                        'Asset Service' AS expense_type,
                        NULL AS expense_subtype,
                        servicedate::date AS date_from,
                        nextservicedate::date AS date_to,
                        servicecost AS amount,
                        'Asset Service' AS source
                    FROM asset.asset_service_records  

                    UNION ALL

                    SELECT 
                        id,
                        NULL AS asset_id,
                        expense_type,
                        expense_subtype,
                        date_from::date,
                        date_to::date,
                        amount,
                        'Expense' AS source
                    FROM attendance.expense_master
                    WHERE is_active = 'true'
                      AND office_id = @OfficeId
                )
                SELECT 
                    date_from,
                    date_to,
                    STRING_AGG(DISTINCT COALESCE(expense_type,'N/A'), ', ') AS expense_types,
                    STRING_AGG(DISTINCT COALESCE(expense_subtype,'N/A'), ', ') AS expense_subtypes,
                    STRING_AGG(DISTINCT COALESCE(source,'N/A'), ', ') AS sources,
                    SUM(amount) AS total_amount,
                    COUNT(*) AS total_entries,
                    STRING_AGG(DISTINCT COALESCE(asset_id::text,'-'), ', ') AS asset_ids
                FROM unified
                GROUP BY date_from, date_to
                ORDER BY date_from, date_to;
            ";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OfficeId", officeId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new
                            {
                                DateFrom = reader["date_from"],
                                DateTo = reader["date_to"],
                                ExpenseTypes = reader["expense_types"],
                                ExpenseSubtypes = reader["expense_subtypes"],
                                Sources = reader["sources"],
                                TotalAmount = reader["total_amount"],
                                TotalEntries = reader["total_entries"],
                                AssetIds = reader["asset_ids"]
                            });
                        }
                    }
                }
            }

            return Ok(result);
        }
        [HttpGet("report/{officeId}")]
        public IActionResult GetExpenseReportForPie(int officeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var result = new List<object>();
            string connString = _configuration.GetConnectionString("DefaultConnection");

            string query = @"
        WITH unified AS (
           SELECT 
    'Spare Part Service' AS expense_type,
    spare_id::text AS expense_subtype,  
    issue_date::date AS date_from,       
    COALESCE(actual_return_date::date, expected_return_date::date) AS date_to, 
    net_cost AS amount                  
FROM asset.asset_spare_maintenance

            UNION ALL

            SELECT 
                'Asset Service' AS expense_type,
                am.asset_name AS expense_subtype,  
                sr.servicedate::date AS date_from,
                sr.nextservicedate::date AS date_to,
                sr.servicecost AS amount
            FROM asset.asset_service_records sr
            LEFT JOIN asset.asset_master am ON sr.assetid = am.asset_id 

            UNION ALL

            SELECT 
                expense_type,
                expense_subtype,
                date_from::date,
                date_to::date,
                amount
            FROM attendance.expense_master
            WHERE is_active = 'true'
              AND office_id = @OfficeId
        )
        SELECT 
            expense_type,
            COALESCE(expense_subtype, 'N/A') AS expense_subtype,
            SUM(COALESCE(amount, 0)) AS total_amount
        FROM unified
        WHERE date_from >= @StartDate AND date_to <= @EndDate
        GROUP BY expense_type, expense_subtype
        ORDER BY total_amount DESC;
    ";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@OfficeId", officeId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new
                            {
                                ExpenseType = reader["expense_type"].ToString(),
                                ExpenseSubType = reader["expense_subtype"].ToString(),
                                TotalAmount = reader["total_amount"] == DBNull.Value
    ? 0
    : Convert.ToDecimal(reader["total_amount"])
                            });
                        }
                    }
                }
            }

            return Ok(result);
        }

    }
}
