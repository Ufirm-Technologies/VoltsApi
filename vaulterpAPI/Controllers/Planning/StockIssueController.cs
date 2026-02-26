using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Planning;   // ✅ Import the DTO

namespace vaulterpAPI.Controllers.Planning
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockIssueController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StockIssueController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        [HttpGet("get-issued-stocks")]
        public async Task<IActionResult> GetIssuedStocks([FromQuery] int officeId, [FromQuery] DateTime date)
        {
            var stocks = new List<StockIssueFullDTO>();

            var query = @"
        SELECT 
            b.id,
            b.inwo,
            b.jobcard_id,
            b.operation,
            b.employee_id,
            b.item_id,
            b.quantity_issued,
            b.created_by,
            b.created_on,
            b.is_active,
            b.office_id,
            COALESCE(SUM(a.quantity_received), 0) AS total_quantity
        FROM planning.item_issue b
        LEFT JOIN inventory.scanned_po_data a 
            ON a.item_id = b.item_id
        WHERE CAST(b.created_on AS DATE) <= @Date
          AND b.office_id = @OfficeId And b.is_active = TRUE
        GROUP BY b.id, b.inwo, b.jobcard_id, b.operation, b.employee_id,
                 b.item_id, b.quantity_issued, b.created_by, b.created_on,
                 b.is_active, b.office_id;";

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Date", DateTime.Today);
            cmd.Parameters.AddWithValue("@OfficeId", officeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stocks.Add(new StockIssueFullDTO
                {
                    Id = reader.GetInt32(0),
                    Inwo = reader.GetInt32(1),
                    JobcardId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Operation = reader.GetString(3),
                    EmployeeId = reader.GetInt32(4),
                    ItemId = reader.GetInt32(5),
                    QuantityIssued = reader.GetInt32(6),
                    CreatedBy = reader.GetInt32(7),
                    CreatedOn = reader.GetDateTime(8),
                    IsActive = reader.GetBoolean(9),
                    OfficeId = reader.GetInt32(10),
                    TotalQuantity = reader.GetInt32(11)
                });
            }

            return Ok(stocks);
        }
    }
}
