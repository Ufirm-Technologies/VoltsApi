using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Payroll;

namespace vaulterpAPI.Controllers.payroll
{
    [ApiController]
    [Route("api/[controller]")]
    public class AllowanceDedutionController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AllowanceDedutionController(IConfiguration config)
        {
            _config = config;
        }

        private string ConnStr() => _config.GetConnectionString("DefaultConnection");


        // ----------------------------------
        // GET ALL BY OFFICE
        // ----------------------------------
        [HttpGet("by-office/{officeId}")]
        public async Task<IActionResult> GetByOffice(int officeId)
        {
            var list = new List<AllowanceDedution>();

            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();

            var query = @"
                SELECT *
                FROM payroll.allowance_dedution
                WHERE office_id = @officeId AND is_active = TRUE
                ORDER BY id ASC;
            ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@officeId", officeId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapReader(reader));
            }

            return Ok(list);
        }


        // ----------------------------------
        // GET BY ID
        // ----------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();

            var query = @"SELECT * FROM payroll.allowance_dedution WHERE id=@id;";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return Ok(MapReader(reader));

            return NotFound(new { message = "Record not found" });
        }


        // ----------------------------------
        // CREATE
        // ----------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AllowanceDedution model)
        {
            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();

            var query = @"
                INSERT INTO payroll.allowance_dedution
                    (type, name, office_id, created_on, created_by, is_active)
                VALUES
                    (@type, @name, @officeId, CURRENT_TIMESTAMP, @createdBy, TRUE)
                RETURNING id;
            ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@type", model.Type);
            cmd.Parameters.AddWithValue("@name", model.Name);
            cmd.Parameters.AddWithValue("@officeId", model.OfficeId);
            cmd.Parameters.AddWithValue("@createdBy", (object?)model.CreatedBy ?? DBNull.Value);

            model.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            model.IsActive = true;
            model.CreatedOn = DateTime.UtcNow;

            return Ok(new { message = "Created successfully", data = model });
        }


        // ----------------------------------
        // UPDATE
        // ----------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AllowanceDedution model)
        {
            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();

            var query = @"
                UPDATE payroll.allowance_dedution
                SET type = @type,
                    name = @name,
                    office_id = @officeId,
                    updated_by = @updatedBy,
                    updated_on = CURRENT_TIMESTAMP
                WHERE id = @id;
            ";

            using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@type", model.Type);
            cmd.Parameters.AddWithValue("@name", model.Name);
            cmd.Parameters.AddWithValue("@officeId", model.OfficeId);
            cmd.Parameters.AddWithValue("@updatedBy", (object?)model.UpdatedBy ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Record not found" });

            return Ok(new { message = "Updated successfully" });
        }


        // ----------------------------------
        // SOFT DELETE
        // ----------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new NpgsqlConnection(ConnStr());
            await conn.OpenAsync();

            var query = @"
                UPDATE payroll.allowance_dedution
                SET is_active = FALSE,
                    updated_on = CURRENT_TIMESTAMP
                WHERE id=@id AND is_active=TRUE;
            ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Record not found or already inactive" });

            return Ok(new { message = "Deleted successfully" });
        }


        // ----------------------------------
        // READER → MODEL MAPPER
        // ----------------------------------
        private AllowanceDedution MapReader(NpgsqlDataReader r)
        {
            return new AllowanceDedution
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Type = r.GetString(r.GetOrdinal("type")),
                Name = r.GetString(r.GetOrdinal("name")),
                OfficeId = r.GetInt32(r.GetOrdinal("office_id")),

                CreatedOn = r.IsDBNull(r.GetOrdinal("created_on")) ? null : r.GetDateTime(r.GetOrdinal("created_on")),
                CreatedBy = r.IsDBNull(r.GetOrdinal("created_by")) ? null : r.GetInt32(r.GetOrdinal("created_by")),

                UpdatedOn = r.IsDBNull(r.GetOrdinal("updated_on")) ? null : r.GetDateTime(r.GetOrdinal("updated_on")),
                UpdatedBy = r.IsDBNull(r.GetOrdinal("updated_by")) ? null : r.GetInt32(r.GetOrdinal("updated_by")),

                IsActive = r.GetBoolean(r.GetOrdinal("is_active"))
            };
        }
    }
}
