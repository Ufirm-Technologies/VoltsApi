using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Inventory;

namespace vaulterpAPI.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CategoryController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpGet]
        public async Task<IActionResult> GetAllCategories(int officeId)
        {
            var categories = new List<CategoryDto>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(@"SELECT id, office_id, name, description, is_active, is_approved, created_on, created_by 
                                                FROM inventory.category 
                                                WHERE office_id = @office_id AND is_active = true AND is_approved = true", conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new CategoryDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsActive = reader.GetBoolean(4),
                    IsApproved = reader.GetBoolean(5),
                    CreatedOn = reader.GetDateTime(6),
                    CreatedBy = reader.GetInt32(7)
                });
            }

            return categories.Count > 0 ? Ok(categories) : NotFound();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(@"SELECT id, office_id, name, description, is_active, is_approved, created_on, created_by 
                                                FROM inventory.category 
                                                WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var category = new CategoryDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsActive = reader.GetBoolean(4),
                    IsApproved = reader.GetBoolean(5),
                    CreatedOn = reader.GetDateTime(6),
                    CreatedBy = reader.GetInt32(7),
                };
                return Ok(category);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto dto)
        {
            if (dto == null) return BadRequest("Invalid category data.");

            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"INSERT INTO inventory.category 
                            (office_id, name, description, created_by, created_on, is_active, is_approved)
                          VALUES 
                            (@office_id, @name, @description, @created_by, NOW(), true, true)
                          RETURNING id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("@name", dto.Name);
            cmd.Parameters.AddWithValue("@description", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);

            await conn.OpenAsync();
            var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new { message = "Category created successfully", id = insertedId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            if (dto == null) return BadRequest("Invalid category data.");

            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE inventory.category
                          SET name = @name, 
                              description = @description, 
                              is_active = @is_active, 
                              is_approved = @is_approved, 
                              approved_by = @approved_by
                          WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", dto.Name);
            cmd.Parameters.AddWithValue("@description", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_active", dto.IsActive);
            cmd.Parameters.AddWithValue("@is_approved", dto.IsApproved);
            cmd.Parameters.AddWithValue("@approved_by", (object?)dto.ApprovedBy ?? DBNull.Value);

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            return rowsAffected > 0
                ? Ok(new { message = "Category updated successfully" })
                : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());

            var query = @"UPDATE inventory.category 
                          SET is_active = false 
                          WHERE id = @id AND is_active = true";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            return rowsAffected > 0
                ? Ok(new { message = "Category deleted (soft) successfully" })
                : NotFound();
        }

        [HttpGet("pending-approval")]
        public async Task<IActionResult> GetPendingApprovalCategories(int officeId)
        {
            var categories = new List<CategoryDto>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(@"SELECT id, office_id, name, description, is_active, is_approved, created_on, created_by
                                                FROM inventory.category
                                                WHERE office_id = @office_id AND is_active = true AND is_approved = false", conn);
            cmd.Parameters.AddWithValue("@office_id", officeId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new CategoryDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsActive = reader.GetBoolean(4),
                    IsApproved = reader.GetBoolean(5),
                    CreatedOn = reader.GetDateTime(6),
                    CreatedBy = reader.GetInt32(7),
                });
            }

            return Ok(categories);
        }
    }
}
