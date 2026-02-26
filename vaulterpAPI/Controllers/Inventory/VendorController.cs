using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models;

namespace vaulterpAPI.Controllers.Inventory
{
    [ApiController]
    [Route("api/inventory/[controller]")]
    public class VendorController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public VendorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpGet]
        public async Task<IActionResult> GetAllVendors(int officeId)
        {
            var vendors = new List<VendorDto>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(@"
                SELECT id, office_id, name, contact_person, contact_number, email, address,
                       gst_number, pan_number, is_approved, approved_by, created_by, created_on, is_active
                FROM inventory.vendor
                WHERE office_id = @office_id AND is_active = true AND is_approved = true", conn);

            cmd.Parameters.AddWithValue("@office_id", officeId);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                vendors.Add(new VendorDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ContactNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                    GSTNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                    PANNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IsApproved = reader.GetBoolean(9),
                    ApprovedBy = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                    CreatedBy = reader.GetInt32(11),
                    CreatedOn = reader.GetDateTime(12),
                    IsActive = reader.GetBoolean(13)
                });
            }

            return vendors.Count > 0 ? Ok(vendors) : NotFound();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVendorById(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(@"
                SELECT id, office_id, name, contact_person, contact_number, email, address,
                       gst_number, pan_number, is_approved, approved_by, created_by, created_on, is_active
                FROM inventory.vendor
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var vendor = new VendorDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ContactNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                    GSTNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                    PANNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IsApproved = reader.GetBoolean(9),
                    ApprovedBy = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                    CreatedBy = reader.GetInt32(11),
                    CreatedOn = reader.GetDateTime(12),
                    IsActive = reader.GetBoolean(13)
                };
                return Ok(vendor);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] VendorDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                INSERT INTO inventory.vendor
                (office_id, name, contact_person, contact_number, email, address, gst_number, pan_number,
                 created_by, created_on, is_active, is_approved)
                VALUES (@office_id, @name, @contact_person, @contact_number, @email, @address, @gst_number, @pan_number,
                        @created_by, current_timestamp, true, true)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);
            cmd.Parameters.AddWithValue("@name", dto.Name);
            cmd.Parameters.AddWithValue("@contact_person", (object?)dto.ContactPerson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contact_number", (object?)dto.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)dto.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@address", (object?)dto.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@gst_number", (object?)dto.GSTNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pan_number", (object?)dto.PANNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);

            await conn.OpenAsync();
            var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new { message = "Vendor created successfully", id = insertedId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] VendorDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                UPDATE inventory.vendor SET
                    name = @name,
                    contact_person = @contact_person,
                    contact_number = @contact_number,
                    email = @email,
                    address = @address,
                    gst_number = @gst_number,
                    pan_number = @pan_number,
                    is_approved = @is_approved,
                    approved_by = @approved_by
                WHERE id = @id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", dto.Name);
            cmd.Parameters.AddWithValue("@contact_person", (object?)dto.ContactPerson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contact_number", (object?)dto.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)dto.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@address", (object?)dto.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@gst_number", (object?)dto.GSTNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pan_number", (object?)dto.PANNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_approved", dto.IsApproved);
            cmd.Parameters.AddWithValue("@approved_by", (object?)dto.ApprovedBy ?? DBNull.Value);

            await conn.OpenAsync();
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Ok(new { message = "Vendor updated successfully" }) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = "UPDATE inventory.vendor SET is_active = false WHERE id = @id AND is_active = true";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? Ok(new { message = "Vendor deleted (soft) successfully" }) : NotFound();
        }

        [HttpGet("pendingApproval")]
        public async Task<IActionResult> GetPendingApprovalVendors(int office_id)
        {
            var pendingVendors = new List<VendorDto>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(@"
                SELECT id, office_id, name, contact_person, contact_number, email, address,
                       gst_number, pan_number, is_approved, approved_by, created_by, created_on, is_active
                FROM inventory.vendor
                WHERE office_id = @office_id AND is_active = true AND is_approved = false", conn);

            cmd.Parameters.AddWithValue("@office_id", office_id);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pendingVendors.Add(new VendorDto
                {
                    Id = reader.GetInt32(0),
                    OfficeId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ContactNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                    GSTNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                    PANNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IsApproved = reader.GetBoolean(9),
                    ApprovedBy = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                    CreatedBy = reader.GetInt32(11),
                    CreatedOn = reader.GetDateTime(12),
                    IsActive = reader.GetBoolean(13)
                });
            }

            return Ok(pendingVendors);
        }
    }
}
