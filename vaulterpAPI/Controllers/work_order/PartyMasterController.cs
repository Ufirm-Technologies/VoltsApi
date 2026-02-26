using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.work_order;

namespace vaulterpAPI.Controllers.work_order
{
    [ApiController]
    [Route("api/workOrder/[controller]")]
    public class PartyMasterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PartyMasterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ GET by Office ID
        [HttpGet("office/{officeId}")]
        public IActionResult GetByOfficeId(int officeId)
        {
            List<PartyMaster> list = new List<PartyMaster>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM work_order.partymaster WHERE office_id = @officeId AND is_active = true", conn);
            cmd.Parameters.AddWithValue("officeId", officeId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PartyMaster
                {
                    id = Convert.ToInt32(reader["id"]),
                    office_id = Convert.ToInt32(reader["office_id"]),
                    name = reader["name"]?.ToString(),
                    contact_person = reader["contact_person"]?.ToString(),
                    contact_number = reader["contact_number"]?.ToString(),
                    email = reader["email"]?.ToString(),
                    address = reader["address"]?.ToString(),
                    gst_number = reader["gst_number"]?.ToString(),
                    pan_number = reader["pan_number"]?.ToString(),
                    is_approved = Convert.ToBoolean(reader["is_approved"]),
                    approved_by = reader["approved_by"] as int?,
                    created_by = reader["created_by"] as int?,
                    created_on = Convert.ToDateTime(reader["created_on"]),
                    is_active = Convert.ToBoolean(reader["is_active"]),
                    pan_url = reader["pan_url"]?.ToString(),
                    gst_certificate_url = reader["gst_certificate_url"]?.ToString(),
                    company_brochure_url = reader["company_brochure_url"]?.ToString(),
                    website_url = reader["website_url"]?.ToString()
                });
            }

            return Ok(list);
        }

        // ✅ GET by ID
        [HttpGet("id/{id}")]
        public IActionResult GetById(int id)
        {
            PartyMaster? data = null;
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT * FROM work_order.partymaster WHERE id = @id AND is_active = true", conn);
                cmd.Parameters.AddWithValue("id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    data = new PartyMaster
                    {
                        id = Convert.ToInt32(reader["id"]),
                        office_id = Convert.ToInt32(reader["office_id"]),
                        name = reader["name"]?.ToString(),
                        contact_person = reader["contact_person"]?.ToString(),
                        contact_number = reader["contact_number"]?.ToString(),
                        email = reader["email"]?.ToString(),
                        address = reader["address"]?.ToString(),
                        gst_number = reader["gst_number"]?.ToString(),
                        pan_number = reader["pan_number"]?.ToString(),
                        is_approved = Convert.ToBoolean(reader["is_approved"]),
                        approved_by = reader["approved_by"] as int?,
                        created_by = reader["created_by"] as int?,
                        created_on = Convert.ToDateTime(reader["created_on"]),
                        is_active = Convert.ToBoolean(reader["is_active"]),
                        pan_url = reader["pan_url"]?.ToString(),
                        gst_certificate_url = reader["gst_certificate_url"]?.ToString(),
                        company_brochure_url = reader["company_brochure_url"]?.ToString(),
                        website_url = reader["website_url"]?.ToString()
                    };
                }
            }
            return data == null ? NotFound() : Ok(data);
        }

        // ✅ POST
        [HttpPost]
        public IActionResult Create([FromBody] PartyMaster model)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            string query = @"
                INSERT INTO work_order.partymaster 
                (office_id, name, contact_person, contact_number, email, address, gst_number, pan_number, 
                is_approved, approved_by, created_by, created_on, is_active, pan_url, gst_certificate_url, 
                company_brochure_url, website_url)
                VALUES 
                (@office_id, @name, @contact_person, @contact_number, @email, @address, @gst_number, @pan_number, 
                @is_approved, @approved_by, @created_by, @created_on, @is_active, @pan_url, @gst_certificate_url, 
                @company_brochure_url, @website_url)";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("office_id", model.office_id);
            cmd.Parameters.AddWithValue("name", model.name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("contact_person", model.contact_person ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("contact_number", model.contact_number ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("email", model.email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("address", model.address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gst_number", model.gst_number ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("pan_number", model.pan_number ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("is_approved", model.is_approved);
            cmd.Parameters.AddWithValue("approved_by", model.approved_by ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("created_by", model.created_by ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("created_on", model.created_on);
            cmd.Parameters.AddWithValue("is_active", model.is_active);
            cmd.Parameters.AddWithValue("pan_url", model.pan_url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gst_certificate_url", model.gst_certificate_url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("company_brochure_url", model.company_brochure_url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("website_url", model.website_url ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
            return Ok(new { message = "Party created successfully" });
        }

        // ✅ PUT (Update)
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] PartyMaster model)
        {
            if (id != model.id)
                return BadRequest("ID mismatch.");

            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            string query = @"
                UPDATE work_order.partymaster SET 
                    office_id = @office_id,
                    name = @name,
                    contact_person = @contact_person,
                    contact_number = @contact_number,
                    email = @email,
                    address = @address,
                    gst_number = @gst_number,
                    pan_number = @pan_number,
                    is_approved = @is_approved,
                    approved_by = @approved_by,
                    created_by = @created_by,
                    created_on = @created_on,
                    is_active = @is_active,
                    pan_url = @pan_url,
                    gst_certificate_url = @gst_certificate_url,
                    company_brochure_url = @company_brochure_url,
                    website_url = @website_url
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("office_id", model.office_id);
            cmd.Parameters.AddWithValue("name", model.name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("contact_person", model.contact_person ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("contact_number", model.contact_number ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("email", model.email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("address", model.address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gst_number", model.gst_number ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("pan_number", model.pan_number ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("is_approved", model.is_approved);
            cmd.Parameters.AddWithValue("approved_by", model.approved_by ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("created_by", model.created_by ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("created_on", model.created_on);
            cmd.Parameters.AddWithValue("is_active", model.is_active);
            cmd.Parameters.AddWithValue("pan_url", model.pan_url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gst_certificate_url", model.gst_certificate_url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("company_brochure_url", model.company_brochure_url ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("website_url", model.website_url ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
            return Ok(new { message = "Party updated successfully" });
        }

        // ✅ DELETE (Soft Delete)
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            string query = "UPDATE work_order.partymaster SET is_active = false WHERE id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);
            int rowsAffected = cmd.ExecuteNonQuery();

            return rowsAffected == 0 ? NotFound() : Ok(new { message = "Party deleted (soft) successfully" });
        }

       

    }
}

