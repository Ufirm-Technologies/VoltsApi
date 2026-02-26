using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.Tutorial;

namespace vaulterpAPI.Controllers
{
    [ApiController]
    [Route("api/tutorial")]
    public class TutorialController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TutorialController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // GET: api/tutorial/{activityId}/blocks
        [HttpGet("{activityId}/blocks")]
        public IActionResult GetBlocks(string activityId)
        {
            try
            {
                if (!int.TryParse(activityId, out int tutorialId))
                {
                    return BadRequest(new
                    {
                        message = "Invalid tutorial id"
                    });
                }

                List<TutorialBlockDto> blocks = new();

                using var conn = new NpgsqlConnection(GetConnectionString());
                using var cmd = new NpgsqlCommand(@"
                    SELECT
                        block_type,
                        block_order,
                        block_config
                    FROM tutorials.tutorial_blocks
                    WHERE tutorial_id = @tutorial_id
                    ORDER BY block_order
                ", conn);

                cmd.Parameters.AddWithValue("@tutorial_id", tutorialId);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    blocks.Add(new TutorialBlockDto
                    {
                        BlockType = reader["block_type"]?.ToString(),
                        BlockOrder = Convert.ToInt32(reader["block_order"]),
                        BlockConfig = reader["block_config"]
                    });
                }

                return Ok(blocks);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BLOCK API ERROR: " + ex.Message);

                return StatusCode(500, new
                {
                    message = "Failed to load blocks",
                    error = ex.Message
                });
            }
        }
    }
}
