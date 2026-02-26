using Microsoft.AspNetCore.Mvc;
using Npgsql;
using vaulterpAPI.Models.work_order;

namespace vaulterpAPI.Controllers.work_order
{
    [ApiController]
    [Route("api/work_order/[controller]")]
    public class WorkOrderMilestoneController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public WorkOrderMilestoneController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString() =>
            _configuration.GetConnectionString("DefaultConnection");

        // POST
        [HttpPost]
        public IActionResult Create([FromBody] WorkOrderMilestone milestone)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                INSERT INTO work_order.work_order_milestones
                (woid, date, target, createdby, createdon, is_active)
                VALUES (@woid, @date, @target, @createdby, @createdon, @is_active)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("@woid", milestone.Woid);
            cmd.Parameters.AddWithValue("@date", milestone.Date);
            cmd.Parameters.AddWithValue("@target", milestone.Target);
            cmd.Parameters.AddWithValue("@createdby", milestone.CreatedBy ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@createdon", milestone.CreatedOn ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@is_active", milestone.IsActive);

            var id = Convert.ToInt32(cmd.ExecuteScalar());
            return Ok(new { message = "Milestone created", id });
        }

        // GET by Work Order ID
        [HttpGet("woid/{woid}")]
        public IActionResult GetByWorkOrder(int woid)
        {
            var list = new List<WorkOrderMilestone>();
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                SELECT * FROM work_order.work_order_milestones
                WHERE woid = @woid AND is_active = true", conn);

            cmd.Parameters.AddWithValue("@woid", woid);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WorkOrderMilestone
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Woid = reader.GetInt32(reader.GetOrdinal("woid")),
                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                    Target = reader.GetInt32(reader.GetOrdinal("target")),
                    CreatedBy = reader["createdby"] as int?,
                    CreatedOn = reader["createdon"] as DateTime?,
                    UpdatedBy = reader["updatedby"] as int?,
                    UpdatedOn = reader["updatedon"] as DateTime?,
                    IsActive = (bool)reader["is_active"]
                });
            }

            return Ok(list);
        }

        // PUT
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] WorkOrderMilestone milestone)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                UPDATE work_order.work_order_milestones
                SET date = @date,
                    target = @target,
                    updatedby = @updatedby,
                    updatedon = @updatedon,
                    is_active = @is_active
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@date", milestone.Date);
            cmd.Parameters.AddWithValue("@target", milestone.Target);
            cmd.Parameters.AddWithValue("@updatedby", milestone.UpdatedBy ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedon", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@is_active", milestone.IsActive);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0) return NotFound(new { message = "Milestone not found" });

            return Ok(new { message = "Milestone updated" });
        }

        // DELETE (Soft Delete)
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                UPDATE work_order.work_order_milestones
                SET is_active = false,
                    updatedon = @updatedon
                WHERE id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@updatedon", DateTime.UtcNow);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0) return NotFound(new { message = "Milestone not found" });

            return Ok(new { message = "Milestone soft-deleted" });
        }
    }
}
