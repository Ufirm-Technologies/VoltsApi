using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Inventory;

namespace vaulterpAPI.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ItemController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpGet]
        public async Task<IActionResult> GetAllItems(int officeId, int? categoryId = null)
        {
            if (officeId <= 0) return BadRequest("OfficeId is required.");
            var items = new List<ItemDto>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"SELECT id, office_id, category_id, name, description, measurement_unit, min_stock_level,
                                 is_active, is_approved, approved_by, created_on, created_by, brand_name, hsn_code
                          FROM inventory.item
                          WHERE office_id = @office_id AND is_active = true AND is_approved = true";

            if (categoryId.HasValue) query += " AND category_id = @category_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            if (categoryId.HasValue) cmd.Parameters.AddWithValue("@category_id", categoryId.Value);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new ItemDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    CategoryId = reader.GetInt32(2),
                    Name = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    MeasurementUnit = reader.IsDBNull(5) ? null : reader.GetString(5),
                    MinStockLevel = reader.GetInt32(6),
                    IsActive = reader.GetBoolean(7),
                    IsApproved = reader.GetBoolean(8),
                    ApprovedBy = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                    CreatedOn = reader.GetDateTime(10),
                    CreatedBy = reader.GetInt32(11),
                    BrandName = reader.IsDBNull(12) ? null : reader.GetString(12),
                    HSNCode = reader.IsDBNull(13) ? null : reader.GetString(13)
                });
            }

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(@"SELECT id, office_id, category_id, name, description, measurement_unit, min_stock_level,
                                                  is_active, is_approved, approved_by, created_on, created_by, brand_name, hsn_code
                                           FROM inventory.item
                                           WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var item = new ItemDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    CategoryId = reader.GetInt32(2),
                    Name = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    MeasurementUnit = reader.IsDBNull(5) ? null : reader.GetString(5),
                    MinStockLevel = reader.GetInt32(6),
                    IsActive = reader.GetBoolean(7),
                    IsApproved = reader.GetBoolean(8),
                    ApprovedBy = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                    CreatedOn = reader.GetDateTime(10),
                    CreatedBy = reader.GetInt32(11),
                    BrandName = reader.IsDBNull(12) ? null : reader.GetString(12),
                    HSNCode = reader.IsDBNull(13) ? null : reader.GetString(13)
                };
                return Ok(item);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] ItemDto dto)
        {
            if (dto == null) return BadRequest("Invalid item data.");
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"INSERT INTO inventory.item 
                          (office_id, category_id, name, description, measurement_unit, min_stock_level, created_by, created_on, is_active, is_approved, brand_name, hsn_code) 
                          VALUES (@office_id, @category_id, @name, @description, @measurement_unit, @min_stock_level, @created_by, NOW(), true, true, @brand_name, @hsn_code)
                          RETURNING id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("@category_id", dto.CategoryId);
            cmd.Parameters.AddWithValue("@name", dto.Name);
            cmd.Parameters.AddWithValue("@description", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@measurement_unit", (object?)dto.MeasurementUnit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@min_stock_level", dto.MinStockLevel);
            cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);
            cmd.Parameters.AddWithValue("@brand_name", (object?)dto.BrandName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hsn_code", (object?)dto.HSNCode ?? DBNull.Value);

            await conn.OpenAsync();
            var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new { message = "Item created successfully", id = insertedId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemDto dto)
        {
            if (dto == null) return BadRequest("Invalid item data.");
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE inventory.item 
                          SET name = @name,
                              description = @description,
                              category_id = @category_id,
                              measurement_unit = @measurement_unit,
                              min_stock_level = @min_stock_level,
                              is_active = @is_active,
                              is_approved = @is_approved,
                              approved_by = @approved_by,
                              brand_name = @brand_name,
                              hsn_code = @hsn_code
                          WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", dto.Name);
            cmd.Parameters.AddWithValue("@description", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@category_id", dto.CategoryId);
            cmd.Parameters.AddWithValue("@measurement_unit", (object?)dto.MeasurementUnit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@min_stock_level", dto.MinStockLevel);
            cmd.Parameters.AddWithValue("@is_active", dto.IsActive);
            cmd.Parameters.AddWithValue("@is_approved", dto.IsApproved);
            cmd.Parameters.AddWithValue("@approved_by", (object?)dto.ApprovedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@brand_name", (object?)dto.BrandName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hsn_code", (object?)dto.HSNCode ?? DBNull.Value);

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            return rowsAffected > 0 ? Ok(new { message = "Item updated successfully" }) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE inventory.item
                          SET is_active = false
                          WHERE id = @id AND is_active = true";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            return rowsAffected > 0
                ? Ok(new { message = "Item deleted (soft) successfully" })
                : NotFound();
        }

        [HttpGet("pending-approval")]
        public async Task<IActionResult> GetUnapprovedItems(int officeId)
        {
            if (officeId <= 0) return BadRequest("OfficeId is required.");
            var items = new List<ItemDto>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"SELECT id, office_id, category_id, name, description, measurement_unit, min_stock_level,
                                 is_active, is_approved, approved_by, created_on, created_by, brand_name, hsn_code
                          FROM inventory.item
                          WHERE office_id = @office_id AND is_active = true AND is_approved = false";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new ItemDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    CategoryId = reader.GetInt32(2),
                    Name = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    MeasurementUnit = reader.IsDBNull(5) ? null : reader.GetString(5),
                    MinStockLevel = reader.GetInt32(6),
                    IsActive = reader.GetBoolean(7),
                    IsApproved = reader.GetBoolean(8),
                    ApprovedBy = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                    CreatedOn = reader.GetDateTime(10),
                    CreatedBy = reader.GetInt32(11),
                    BrandName = reader.IsDBNull(12) ? null : reader.GetString(12),
                    HSNCode = reader.IsDBNull(13) ? null : reader.GetString(13)
                });
            }

            return Ok(items);
        }
    }
}
