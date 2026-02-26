using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Complaint;

namespace vaulterpAPI.Controllers.Complaint
{
    [ApiController]
    [Route("api/complaint/[controller]")]
    public class TicketTypeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TicketTypeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ GET all ticket types
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = new List<TicketTypeDto>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"SELECT * FROM complaint.tickettype WHERE isdeleted = 0";

            using var cmd = new NpgsqlCommand(query, conn);
            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TicketTypeDto
                {
                    TicketTypeId = reader.GetInt32(reader.GetOrdinal("tickettypeid")),
                    Type = reader.IsDBNull(reader.GetOrdinal("type")) ? null : reader.GetString(reader.GetOrdinal("type")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    Status = reader.GetInt32(reader.GetOrdinal("status")),
                    CreatedBy = reader.GetInt32(reader.GetOrdinal("createdby")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    IsDeleted = reader.GetInt32(reader.GetOrdinal("isdeleted"))
                });
            }

            return Ok(list);
        }

        // ✅ GET by id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            TicketTypeDto? ticketType = null;
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"SELECT * FROM complaint.tickettype WHERE tickettypeid = @id AND isdeleted = 0";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                ticketType = new TicketTypeDto
                {
                    TicketTypeId = reader.GetInt32(reader.GetOrdinal("tickettypeid")),
                    Type = reader.IsDBNull(reader.GetOrdinal("type")) ? null : reader.GetString(reader.GetOrdinal("type")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    Status = reader.GetInt32(reader.GetOrdinal("status")),
                    CreatedBy = reader.GetInt32(reader.GetOrdinal("createdby")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    IsDeleted = reader.GetInt32(reader.GetOrdinal("isdeleted"))
                };
            }

            if (ticketType == null)
                return NotFound(new { message = "TicketType not found" });

            return Ok(ticketType);
        }

        // ✅ POST (Insert)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TicketTypeDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                INSERT INTO complaint.tickettype (type, description, status, createdby, createdon, isdeleted)
                VALUES (@type, @description, @status, @createdby, NOW(), 0)
                RETURNING tickettypeid";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@type", (object?)dto.Type ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", dto.Status);
            cmd.Parameters.AddWithValue("@createdby", dto.CreatedBy);

            await conn.OpenAsync();
            var newId = await cmd.ExecuteScalarAsync();

            return Ok(new { message = "Inserted successfully", ticketTypeId = newId });
        }

        // ✅ PUT (Update by Id)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TicketTypeDto dto)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"
                UPDATE complaint.tickettype
                SET type = @type,
                    description = @description,
                    status = @status,
                    isdeleted = @isdeleted
                WHERE tickettypeid = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@type", (object?)dto.Type ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", dto.Status);
            cmd.Parameters.AddWithValue("@isdeleted", dto.IsDeleted);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0) return NotFound(new { message = "No record found" });
            return Ok(new { message = "Updated successfully" });
        }

        // ✅ DELETE (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            var query = @"UPDATE complaint.tickettype SET isdeleted = 1 WHERE tickettypeid = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0) return NotFound(new { message = "No record found" });
            return Ok(new { message = "Deleted successfully" });
        }
    }
}
