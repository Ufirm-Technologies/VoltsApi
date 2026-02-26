using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Planning;

namespace vaulterpAPI.Controllers.Planning
{
    [ApiController]
    [Route("api/planning/[controller]")]
    public class SpecificationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SpecificationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // ✅ GET all specifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Specification>>> GetSpecifications()
        {
            var specs = new List<Specification>();

            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string query = "SELECT id, specification, created_by, created_on FROM planning.specification";
            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                specs.Add(new Specification
                {
                    Id = reader.GetInt32(0),
                    SpecificationName = reader.GetString(1),
                    CreatedBy = reader.GetInt32(2),
                    CreatedOn = reader.GetDateTime(3)
                });
            }

            return Ok(specs);
        }

        // ✅ CREATE new specification
        [HttpPost]
        public async Task<ActionResult> CreateSpecification([FromBody] Specification spec)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string query = @"INSERT INTO planning.specification (specification, created_by, created_on) 
                             VALUES (@specification, @created_by, @created_on) RETURNING id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@specification", spec.SpecificationName);
            cmd.Parameters.AddWithValue("@created_by", spec.CreatedBy);
            cmd.Parameters.AddWithValue("@created_on", DateTime.UtcNow);

            var newId = (int)await cmd.ExecuteScalarAsync();

            return CreatedAtAction(nameof(GetSpecifications), new { id = newId }, spec);
        }
    }
}
