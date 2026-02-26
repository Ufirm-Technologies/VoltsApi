using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.OfficeMaster;

namespace vaulterpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfficeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public OfficeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                List<OfficeDto> offices = new();

                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand("SELECT * FROM master.office_master WHERE is_active = true", conn);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    offices.Add(new OfficeDto
                    {
                        OfficeId = Convert.ToInt32(reader["office_id"]),
                        OfficeName = reader["office_name"]?.ToString(),
                        OfficeType = reader["office_type"]?.ToString(),
                        Region = reader["region"]?.ToString(),
                        AddressLine1 = reader["address_line1"]?.ToString(),
                        AddressLine2 = reader["address_line2"]?.ToString(),
                        City = reader["city"]?.ToString(),
                        State = reader["state"]?.ToString(),
                        Pincode = reader["pincode"]?.ToString(),
                        ContactNumber = reader["contact_number"]?.ToString(),
                        Email = reader["email"]?.ToString(),
                        Latitude = reader["latitude"] != DBNull.Value ? Convert.ToDecimal(reader["latitude"]) : 0,
                        Longitude = reader["longitude"] != DBNull.Value ? Convert.ToDecimal(reader["longitude"]) : 0,
                        IsActive = Convert.ToBoolean(reader["is_active"])
                    });
                }

                return Ok(offices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching offices", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Create(OfficeDto office)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO master.office_master 
                    (office_name, office_type, region, address_line1, address_line2, city, state, pincode,
                     contact_number, email, latitude, longitude, created_by, created_on, is_active)
                    VALUES
                    (@office_name, @office_type, @region, @address_line1, @address_line2, @city, @state, @pincode,
                     @contact_number, @email, @latitude, @longitude, @created_by, NOW(), true)", conn);

                cmd.Parameters.AddWithValue("@office_name", office.OfficeName ?? "");
                cmd.Parameters.AddWithValue("@office_type", office.OfficeType ?? "");
                cmd.Parameters.AddWithValue("@region", office.Region ?? "");
                cmd.Parameters.AddWithValue("@address_line1", office.AddressLine1 ?? "");
                cmd.Parameters.AddWithValue("@address_line2", office.AddressLine2 ?? "");
                cmd.Parameters.AddWithValue("@city", office.City ?? "");
                cmd.Parameters.AddWithValue("@state", office.State ?? "");
                cmd.Parameters.AddWithValue("@pincode", office.Pincode ?? "");
                cmd.Parameters.AddWithValue("@contact_number", office.ContactNumber ?? "");
                cmd.Parameters.AddWithValue("@email", office.Email ?? "");
                cmd.Parameters.AddWithValue("@latitude", office.Latitude);
                cmd.Parameters.AddWithValue("@longitude", office.Longitude);
                cmd.Parameters.AddWithValue("@created_by", office.CreatedBy);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { message = "Office created successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating office", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, OfficeDto office)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    UPDATE master.office_master SET
                        office_name = @office_name,
                        office_type = @office_type,
                        region = @region,
                        address_line1 = @address_line1,
                        address_line2 = @address_line2,
                        city = @city,
                        state = @state,
                        pincode = @pincode,
                        contact_number = @contact_number,
                        email = @email,
                        latitude = @latitude,
                        longitude = @longitude,
                        modified_by = @modified_by,
                        modified_on = NOW()
                    WHERE office_id = @office_id", conn);

                cmd.Parameters.AddWithValue("@office_id", id);
                cmd.Parameters.AddWithValue("@office_name", office.OfficeName ?? "");
                cmd.Parameters.AddWithValue("@office_type", office.OfficeType ?? "");
                cmd.Parameters.AddWithValue("@region", office.Region ?? "");
                cmd.Parameters.AddWithValue("@address_line1", office.AddressLine1 ?? "");
                cmd.Parameters.AddWithValue("@address_line2", office.AddressLine2 ?? "");
                cmd.Parameters.AddWithValue("@city", office.City ?? "");
                cmd.Parameters.AddWithValue("@state", office.State ?? "");
                cmd.Parameters.AddWithValue("@pincode", office.Pincode ?? "");
                cmd.Parameters.AddWithValue("@contact_number", office.ContactNumber ?? "");
                cmd.Parameters.AddWithValue("@email", office.Email ?? "");
                cmd.Parameters.AddWithValue("@latitude", office.Latitude);
                cmd.Parameters.AddWithValue("@longitude", office.Longitude);
                cmd.Parameters.AddWithValue("@modified_by", office.ModifiedBy ?? 1);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { message = "Office updated successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating office", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand("UPDATE master.office_master SET is_active = false WHERE office_id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { message = "Office deleted successfully", status = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting office", error = ex.Message });
            }
        }
    }
}
