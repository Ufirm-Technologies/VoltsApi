using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models;

namespace vaulterpAPI.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/[controller]")]
    public class RateCardController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RateCardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpPost]
        public async Task<IActionResult> CreateRateCard([FromBody] RateCardDto dto)
        {
            if (dto == null || dto.ItemId <= 0 || dto.VendorId <= 0 || dto.Price <= 0)
                return BadRequest("Invalid input data.");

            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"INSERT INTO inventory.ratecard 
                            (item_id, vendor_id, price, valid_till, created_by, is_approved, is_active) 
                          VALUES 
                            (@ItemId, @VendorId, @Price, @ValidTill, @CreatedBy, @IsApproved, TRUE)
                          RETURNING id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ItemId", dto.ItemId);
            cmd.Parameters.AddWithValue("@VendorId", dto.VendorId);
            cmd.Parameters.AddWithValue("@Price", dto.Price);
            cmd.Parameters.AddWithValue("@ValidTill", (object?)dto.ValidTill ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", 1);
            cmd.Parameters.AddWithValue("@IsApproved", true);

            await conn.OpenAsync();
            var insertedId = (int)(await cmd.ExecuteScalarAsync())!;

            return Ok(new { message = "Rate card created successfully.", Id = insertedId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRateCards([FromQuery] int? officeId = null,
                                                         [FromQuery] int? categoryId = null,
                                                         [FromQuery] int? itemId = null,
                                                         [FromQuery] int? vendorId = null)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                SELECT rc.id, 
                       cat.name AS category_name,
                       i.id AS item_id,
                       i.name AS item_name,
                       i.brand_name,
                       i.description,
                       i.hsn_code,
                       v.id AS vendor_id,
                       v.name AS vendor_name,
                       rc.price,
                       i.measurement_unit,
                       rc.valid_till,
                       rc.is_approved,
                       rc.created_on
                FROM inventory.ratecard rc
                INNER JOIN inventory.item i ON rc.item_id = i.id
                INNER JOIN inventory.vendor v ON rc.vendor_id = v.id
                INNER JOIN inventory.category cat ON i.category_id = cat.id
                WHERE rc.is_approved = TRUE
                  AND rc.is_active = TRUE
                  AND (rc.valid_till IS NULL OR rc.valid_till >= CURRENT_DATE)
                  AND i.is_approved = TRUE AND i.is_active = TRUE
                  AND v.is_approved = TRUE AND v.is_active = TRUE
                  AND cat.is_approved = TRUE AND cat.is_active = TRUE
                  AND (@CategoryId IS NULL OR i.category_id = @CategoryId)
                  AND (@OfficeId IS NULL OR i.office_id = @OfficeId)
                  AND (@ItemId IS NULL OR rc.item_id = @ItemId)
                  AND (@VendorId IS NULL OR rc.vendor_id = @VendorId)
                ORDER BY rc.created_on DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.Add(new NpgsqlParameter("@OfficeId", NpgsqlTypes.NpgsqlDbType.Integer) { Value = (object?)officeId ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("@CategoryId", NpgsqlTypes.NpgsqlDbType.Integer) { Value = (object?)categoryId ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("@ItemId", NpgsqlTypes.NpgsqlDbType.Integer) { Value = (object?)itemId ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("@VendorId", NpgsqlTypes.NpgsqlDbType.Integer) { Value = (object?)vendorId ?? DBNull.Value });


            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var rateCards = new List<RateCardDto>();

            while (await reader.ReadAsync())
            {
                rateCards.Add(new RateCardDto
                {
                    Id = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    ItemId = reader.GetInt32(2),
                    ItemName = reader.GetString(3),
                    BrandName = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                    HSNCode = reader.IsDBNull(6) ? null : reader.GetString(6),
                    VendorId = reader.GetInt32(7),
                    VendorName = reader.GetString(8),
                    Price = reader.GetDecimal(9),
                    MeasurementUnit = reader.IsDBNull(10) ? null : reader.GetString(10),
                    ValidTill = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    IsApproved = reader.GetBoolean(12),
                    CreatedOn = reader.GetDateTime(13)
                });
            }

            return Ok(rateCards);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRateCard(int id, [FromBody] RateCardDto dto)
        {
            if (dto == null) return BadRequest("Invalid rate card data.");
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE inventory.ratecard
                          SET item_id = @ItemId,
                              vendor_id = @VendorId,
                              price = @Price,
                              valid_till = @ValidTill,
                              is_approved = @IsApproved
                          WHERE id = @Id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ItemId", dto.ItemId);
            cmd.Parameters.AddWithValue("@VendorId", dto.VendorId);
            cmd.Parameters.AddWithValue("@Price", dto.Price);
            cmd.Parameters.AddWithValue("@ValidTill", (object?)dto.ValidTill ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsApproved", dto.IsApproved);

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            return rowsAffected > 0
                ? Ok(new { message = "Rate card updated successfully." })
                : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRateCard(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE inventory.ratecard 
                          SET is_active = FALSE 
                          WHERE id = @Id AND is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            return rowsAffected > 0
                ? Ok(new { message = "Rate card deleted (soft) successfully" })
                : NotFound();
        }

        [HttpGet("pendingApproval")]
        public async Task<IActionResult> GetPendingApprovalRateCards()
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                SELECT rc.id, 
                       cat.name AS category_name,
                       i.id AS item_id,
                       i.name AS item_name,
                       v.id AS vendor_id,
                       v.name AS vendor_name,
                       rc.price,
                       rc.valid_till,
                       rc.is_approved,
                       rc.created_on
                FROM inventory.ratecard rc
                INNER JOIN inventory.item i ON rc.item_id = i.id
                INNER JOIN inventory.vendor v ON rc.vendor_id = v.id
                INNER JOIN inventory.category cat ON i.category_id = cat.id
                WHERE rc.is_approved = FALSE AND rc.is_active = TRUE";

            using var cmd = new NpgsqlCommand(query, conn);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            var rateCards = new List<RateCardDto>();

            while (await reader.ReadAsync())
            {
                rateCards.Add(new RateCardDto
                {
                    Id = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    ItemId = reader.GetInt32(2),
                    ItemName = reader.GetString(3),
                    VendorId = reader.GetInt32(4),
                    VendorName = reader.GetString(5),
                    Price = reader.GetDecimal(6),
                    ValidTill = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    IsApproved = reader.GetBoolean(8),
                    CreatedOn = reader.GetDateTime(9)
                });
            }

            return Ok(rateCards);
        }
    }
}
