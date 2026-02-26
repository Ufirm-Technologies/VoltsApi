using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Inventory;

namespace vaulterpAPI.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StockController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // ✅ GET /api/stock/office?office_id=101
        [HttpGet("office")]
        public async Task<IActionResult> GetStockByOfficeId([FromQuery] int office_id)
        {
            var stockList = new List<StockDto>();

            await using var conn = GetConnection();
            await conn.OpenAsync();

            string query = @"
                SELECT 
                    s.stock_id, 
                    s.item_id, 
                    s.office_id, 
                    s.current_qty, 
                    s.min_qty,
                    i.name,
                    i.description,
                    i.category_id
                FROM 
                    inventory.stock s
                INNER JOIN 
                    inventory.item i ON s.item_id = i.id
                WHERE 
                    s.office_id = @office_id";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", office_id);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stockList.Add(new StockDto
                {
                    stock_id = reader.GetInt32(0),
                    item_id = reader.GetInt32(1),
                    office_id = reader.GetInt32(2),
                    current_qty = reader.GetInt32(3),
                    min_qty = reader.GetInt32(4),
                    name = reader.GetString(5),
                    description = reader.GetString(6),
                    category_id = reader.GetInt32(7)
                });
            }

            return Ok(stockList);
        }

        // ✅ GET /api/stock/by-item?item_id=55
        [HttpGet("by-item")]
        public async Task<IActionResult> GetStockWithVendorByItem([FromQuery] int item_id)
        {
            var stockList = new List<StockWithVendorDto>();

            await using var conn = GetConnection();
            await conn.OpenAsync();

            string query = @"
        SELECT 
            po.po_number,
            s.stock_id, 
            s.item_id, 
            s.office_id, 
SUM(sn.quantity_received),
            s.min_qty,
sn.quantity,
            i.name,
            i.description,
            i.category_id,
            COALESCE(string_agg(DISTINCT v.name, ', '), '') AS vendor_names
        FROM 
            inventory.stock s
        INNER JOIN 
            inventory.item i ON s.item_id = i.id
        LEFT JOIN 
            inventory.purchaseorderitems pod ON pod.itemid = i.id
        LEFT JOIN 
            inventory.purchaseorder po ON po.id = pod.purchaseorderid
        LEFT JOIN 
            inventory.vendor v ON v.id = po.vendor_id
        LEFT JOIN
            inventory.scanned_po_data sn ON sn.item_id = s.item_id
        WHERE 
            s.item_id = @item_id
        GROUP BY 
            s.stock_id, s.item_id, s.office_id, s.current_qty, s.min_qty,
            i.name, i.description, i.category_id, po.po_number, sn.quantity";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@item_id", item_id);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stockList.Add(new StockWithVendorDto
                {
                    po_number = reader.IsDBNull(0) ? null : reader.GetString(0),
                    stock_id = reader.GetInt32(1),
                    item_id = reader.GetInt32(2),
                    office_id = reader.GetInt32(3),
                    current_qty = reader.GetInt32(4),
                    min_qty = reader.GetInt32(5),
                    quantity = reader.GetInt32(6),
                    name = reader.GetString(7),
                    description = reader.GetString(8),
                    category_id = reader.GetInt32(9),
                    vendor_names = reader.GetString(10)
                });
            }

            return Ok(stockList);
        }

        [HttpPost("StockReturn")]
        public async Task<IActionResult> SaveQuantityReturned([FromBody] InventoryReturnDTO model)
        {
            if (model.OfficeId <= 0)
                return BadRequest(new { message = "❌ Office ID is required." });

            if (model.QuantityReturned <= 0)
                return BadRequest(new { message = "❌ Quantity Returned must be greater than zero." });

            await using var con = GetConnection();
            await con.OpenAsync();

            await using var transaction = await con.BeginTransactionAsync();

            try
            {
                // Step 1: Insert negative qty
                string insertQuery = @"
            INSERT INTO inventory.scanned_po_data 
                (po_number, item_name, item_id, quantity_received, received_on, quantity, scan_date_time,barcode_id)
            VALUES 
                (@PoNumber, @ItemName, @ItemId, @QuantityReceived, NOW(), @Quantity, @ScanDateTime,0);";

                await using (var insertCmd = new NpgsqlCommand(insertQuery, con, transaction))
                {
                    insertCmd.Parameters.AddWithValue("@PoNumber", model.PoNumber);
                    insertCmd.Parameters.AddWithValue("@ItemName", model.ItemName);
                    insertCmd.Parameters.AddWithValue("@ItemId", model.ItemId);
                    insertCmd.Parameters.AddWithValue("@Quantity", model.Quantity);
                    insertCmd.Parameters.AddWithValue("@ScanDateTime", model.ScanDateTime ?? DateTime.Now);
                    insertCmd.Parameters.AddWithValue("@QuantityReceived", -model.QuantityReturned);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Step 2: Update stock
                string updateStockQuery = @"
            UPDATE inventory.stock
            SET current_qty = current_qty - @QtyReturned
            WHERE item_id = @ItemId AND office_id = @OfficeId;";

                await using (var updateCmd = new NpgsqlCommand(updateStockQuery, con, transaction))
                {
                    updateCmd.Parameters.AddWithValue("@QtyReturned", model.QuantityReturned);
                    updateCmd.Parameters.AddWithValue("@ItemId", model.ItemId);
                    updateCmd.Parameters.AddWithValue("@OfficeId", model.OfficeId);

                    await updateCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = $"✔ Quantity returned for PO {model.PoNumber}, Item {model.ItemName} saved successfully.",
                    poNumber = model.PoNumber,
                    itemId = model.ItemId,
                    officeId = model.OfficeId,
                    quantityReturned = model.QuantityReturned
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "❌ Error processing request.", error = ex.Message });
            }
        }

        [HttpPost("addStock")]
        public async Task<IActionResult> AddStock([FromBody] AddStockDto stockDto)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Insert/Update stock
                var stockQuery = @"
            INSERT INTO inventory.stock (item_id, office_id, current_qty, min_qty)
            VALUES (@item_id, @office_id, @current_qty, 
                    (SELECT min_stock_level FROM inventory.item WHERE id = @item_id))
            ON CONFLICT (item_id, office_id) 
            DO UPDATE SET 
                current_qty = stock.current_qty + EXCLUDED.current_qty,
                min_qty = EXCLUDED.min_qty;
        ";

                await using (var stockCmd = new NpgsqlCommand(stockQuery, conn, transaction))
                {
                    stockCmd.Parameters.AddWithValue("@item_id", stockDto.item_id);
                    stockCmd.Parameters.AddWithValue("@office_id", stockDto.office_id);
                    stockCmd.Parameters.AddWithValue("@current_qty", stockDto.current_qty);
                    await stockCmd.ExecuteNonQueryAsync();
                }

//                // Insert into scanned_po_data
//                var scannedQuery = @"
//            INSERT INTO inventory.scanned_po_data 
//                ( item_name, item_id, quantity,quantity_received, 
//                 scan_date_time,  received_on)
//            VALUES 
//                ( (SELECT name FROM inventory.item WHERE id = @item_id), 
//                 @item_id,
//(SELECT quantity 
// FROM inventory.scanned_po_data 
// WHERE item_id=@item_id 
// ORDER BY scan_date_time DESC 
// LIMIT 1)
//,
//                 @quantity_received,  
//                 NOW(), 
//                 NOW());
//        ";

//                await using (var scannedCmd = new NpgsqlCommand(scannedQuery, conn, transaction))
//                {
//                    scannedCmd.Parameters.AddWithValue("@item_id", stockDto.item_id);
//                    scannedCmd.Parameters.AddWithValue("@quantity", stockDto.quantity);
//                    scannedCmd.Parameters.AddWithValue("@quantity_received", stockDto.current_qty);

//                    await scannedCmd.ExecuteNonQueryAsync();
//                }

               await transaction.CommitAsync();

                return Ok(new { Message = "Stock and scanned PO data inserted successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { Message = "Error occurred", Details = ex.Message });
            }
        }


        [HttpGet("min-stock-all")]
        public async Task<IActionResult> GetMinStockLevelAll(
      [FromQuery] int office_id,
      [FromQuery] DateTime startDate,
      [FromQuery] DateTime endDate)
        {
            var result = new List<object>();

            await using var conn = GetConnection();
            await conn.OpenAsync();

            string query = @"
    WITH daily_movements AS (
    SELECT 
        s.item_id,
        i.name AS item_name,
        st.office_id,
        s.received_on::date AS tx_date,
        SUM(s.quantity_received) AS daily_received,
        st.min_qty
    FROM inventory.scanned_po_data s
    JOIN inventory.stock st ON st.item_id = s.item_id
    JOIN inventory.item i ON i.id = s.item_id
    WHERE s.received_on::date BETWEEN '2025-01-01' AND '2025-12-31'
    GROUP BY s.item_id, i.name, st.office_id, st.min_qty, s.received_on::date
)
SELECT 
    dm.item_id,
    dm.item_name,
    dm.office_id,
    dm.tx_date,
    dm.daily_received,
    SUM(dm.daily_received) OVER (
        PARTITION BY dm.item_id ORDER BY dm.tx_date
    ) AS running_stock,
    dm.min_qty
FROM daily_movements dm
ORDER BY dm.item_id, dm.tx_date;
    ";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", office_id);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    ItemId = reader.GetInt32(0),
                    ItemName = reader.GetString(1),
                    OfficeId = reader.GetInt32(2),
                    TxDate = reader.GetDateTime(3),
                    DailyReceived = reader.GetInt32(4),
                    RunningStock = reader.GetInt64(5), // SUM() can exceed int range, better use long
                    MinStockLevel = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                    StartDate = startDate,
                    EndDate = endDate
                });
            }

            return Ok(result);
        }


    }
}
